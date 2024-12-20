using Application.CartItems.Commands;
using Application.Common;
using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Authentications.Users;
using Domain.CartItems;
using Domain.Orders;
using MediatR;
using Optional;

namespace Application.Orders.Commands;

public record CreateOrderCommand : IRequest<Result<Order, OrderException>>
{
    public required Guid UserId { get; init; }
    public required string DeliveryAddress { get; init; }
}

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    ICartItemRepository cartItemRepository,
    IProductRepository productRepository)
    : IRequestHandler<CreateOrderCommand, Result<Order, OrderException>>
{
    public async Task<Result<Order, OrderException>> Handle(CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var user = await userRepository.GetById(userId, cancellationToken);

        return await user.Match(
            async u => await CreateEntity(u, request.DeliveryAddress,
                cancellationToken),
            () => Task.FromResult<Result<Order, OrderException>>(
                new OrderUserNotFoundException(userId)));
    }

    private async Task<Result<Order, OrderException>> CreateEntity(User user,
        string requestDeliveryAddress, CancellationToken cancellationToken)
    {
        var userCart = await cartItemRepository.GetByUserId(user.Id, cancellationToken);
        
        if (userCart?.Count() == 0)
        {
            return await Task.FromResult<Result<Order, OrderException>>(new OrderUserCartIsEmpty(user.Id));
        }

        var orderId = OrderId.New();
        
        try
        {
            var order = Order.New(orderId, user.Id, StatusesConstants.Processing, requestDeliveryAddress, userCart!.ToList());
            var orderCreated = await orderRepository.Add(order, cancellationToken);
            
            await productRepository.ChangeStockQuantityForProducts(userCart!.ToList(), cancellationToken);
            
            user.ClearCart();
            await userRepository.Update(user, cancellationToken);
            
            return orderCreated;
        }
        catch (Exception exception)
        {
            return new OrderUnknownException(orderId, exception);
        }
    }
}