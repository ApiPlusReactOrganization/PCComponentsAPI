﻿using System.Linq.Expressions;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Authentications.Users;
using Domain.Products;
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
            .Include(u => u.UserImage)
            .Include(u => u.FavoriteProducts) 
            .Include(u => u.Cart)
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

    public async Task<Option<User>> SearchByEmailForUpdate(UserId userId, string email,
        CancellationToken cancellationToken)
    {
        var entity = await GetUserAsync(x => x.Email == email && x.Id != userId, cancellationToken, true);
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

    public async Task<User?> GetUserAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        if (asNoTracking)
        {
            return await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.UserImage)
                .Include(u => u.Cart)
                .Include(u => u.FavoriteProducts)
                .FirstOrDefaultAsync(predicate, cancellationToken);
        }

        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.UserImage)
            .Include(u => u.Cart)
            .Include(u => u.FavoriteProducts)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetFavoriteProductsByUserId(UserId userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.FavoriteProducts)
            .ThenInclude(p => p.Images)
            .AsNoTracking()                   
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);  

        return user?.FavoriteProducts!;
    }

    public async Task<User> AddFavoriteProduct(UserId userId, Product product, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.FavoriteProducts)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        user!.FavoriteProducts.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    public async Task<User> RemoveFavoriteProduct(UserId userId, Product product, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.FavoriteProducts)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        user!.FavoriteProducts.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}