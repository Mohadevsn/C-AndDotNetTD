using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace P2FixAnAppDotNetCode.Models
{
    /// <summary>
    /// Fournit un panier par visiteur (identifié par sa session).
    /// BUG corrigé : le panier était enregistré en Singleton, donc partagé entre tous les visiteurs.
    /// </summary>
    public static class CartProvider
    {
        private const string CartIdKey = "CartId";
        private static readonly ConcurrentDictionary<string, Cart> Carts = new ConcurrentDictionary<string, Cart>();

        public static ICart GetCart(IServiceProvider services)
        {
            ISession session = services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Session;
            if (session == null)
                return new Cart();

            string cartId = session.GetString(CartIdKey);
            if (cartId == null)
            {
                cartId = Guid.NewGuid().ToString();
                session.SetString(CartIdKey, cartId);
            }

            return Carts.GetOrAdd(cartId, _ => new Cart());
        }
    }
}
