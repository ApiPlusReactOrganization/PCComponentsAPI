﻿using Application.Categories.Exceptions;
using Application.Common;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Categories.Commands;

public record DeleteCategoryCommand : IRequest<Result<Category, CategoryException>>
{
    public required Guid CategoryId { get; init; }
}

public class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<DeleteCategoryCommand, Result<Category, CategoryException>>
{
    public async Task<Result<Category, CategoryException>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var existingCategory = await categoryRepository.GetById(categoryId, cancellationToken);

        return await existingCategory.Match<Task<Result<Category, CategoryException>>>(
            async category => await DeleteEntity(category, cancellationToken),
            () => Task.FromResult<Result<Category, CategoryException>>
                (new CategoryNotFoundException(categoryId)));
    }

    private async Task<Result<Category, CategoryException>> DeleteEntity(
        Category category,
        CancellationToken cancellationToken)
    {
        try
        {
            return await categoryRepository.Delete(category, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23503")
        {
            return new CategoryHasRelatedProductsException(category.Id);
        }
        catch (Exception exception)
        {
            return new CategoryUnknownException(category.Id, exception);
        }
    }
}