using UnityEngine.Purchasing;
using Enums;


namespace Managers.Monetization.IAP
{
    public static class IAPUtils
    {
        public static string GetProductPriceString(EProduct productId)
        {
            return GetProductPriceString(IAPManager.Instance.GetProduct(productId));
        }

        /// <summary>
        /// Returns the localized price string for a product (e.g., "4.99 €", "¥120", etc.)
        /// </summary>
        public static string GetProductPriceString(Product product)
        {
            if (product == null || product.metadata == null)
                return string.Empty;

            return product.metadata.localizedPriceString;
        }
    }
}

