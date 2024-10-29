﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Authentications.Roles;
using Domain.Authentications.Users;
using Microsoft.EntityFrameworkCore;
using Optional;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext _context) : IUserRepository, IUserQueries
{
    public async Task<User> Create(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> Update(User user, CancellationToken cancellationToken)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> Delete(User user, CancellationToken cancellationToken)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<IReadOnlyList<User>> GetAll(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<User>> GetById(UserId id, CancellationToken cancellationToken)
    {
        var entity = await GetUserAsync(x => x.Id == id, cancellationToken, true);

        return entity == null ? Option.None<User>() : Option.Some(entity);
    }

    public async Task<Option<User>> SearchByEmail(string email, CancellationToken cancellationToken)
    {
        var entity = await GetUserAsync(x => x.Email == email, cancellationToken, true);

        return entity == null ? Option.None<User>() : Option.Some(entity);
    }

    public async Task<User> AddRole(UserId userId, string idRole, CancellationToken cancellationToken)
    {
        var entity = await GetUserAsync(x => x.Id == userId, cancellationToken, true);

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == idRole, cancellationToken);

        entity.Roles.Add(role);

        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<User?> GetUserAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken, bool includes = false)
    {
        if (includes)
        {
            return await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }
}