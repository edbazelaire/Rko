using Assets.Scripts.Managers;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.Purchasing;
// using UnityEngine.Purchasing.Extension;   // not needed anymore

namespace Managers.Monetization.IAP
{
    public class IAPManager : MonoBehaviour
    {
        #region Members

        public static IAPManager Instance;

        [Serializable]
        public class ProductInfo
        {
            public EProduct Product;
            public ProductType Type;
        }

        [Header("Products")]
        [SerializeField] private List<ProductInfo> m_ProductsInfo = new List<ProductInfo>() { };

        // v5: use StoreController instead of IStoreController/IExtensionProvider
        private StoreController m_StoreController;

        private Action m_OnPurchaseSuccess;

        public static bool Initialized =>
            Instance != null && Instance.m_StoreController != null;

        #endregion

        #region Init & End

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Entry point used by the rest of your code.
        /// </summary>
        public static void Initialize()
        {
            if (Initialized || Instance == null)
                return;

            // Fire-and-forget async init (Unity recommended pattern for IAP v5)
            Instance.InitializeInternal();
        }

        /// <summary>
        /// Full IAP v5 init flow:
        /// 1) Get StoreController
        /// 2) Subscribe to events
        /// 3) Connect()
        /// 4) FetchProducts(...)
        /// </summary>
        private async void InitializeInternal()
        {
            try
            {
                // 1) Get controller
                m_StoreController = UnityIAPServices.StoreController();

                // 2) Subscribe to events BEFORE connecting
                m_StoreController.OnProductsFetched += OnProductsFetched;
                m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;

                m_StoreController.OnPurchasesFetched += OnPurchasesFetched;

                m_StoreController.OnPurchasePending += OnPurchasePending;
                m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

                // 3) Connect to the store
                await m_StoreController.Connect();
                Debug.Log("[IAP] Connected to store.");

                // 4) Tell IAP which products you care about
                var productDefs = new List<ProductDefinition>();
                foreach (var info in m_ProductsInfo)
                {
                    productDefs.Add(new ProductDefinition(info.Product.ToString(), info.Type));
                }

                m_StoreController.FetchProducts(productDefs);   // triggers OnProductsFetched
            }
            catch (Exception e)
            {
                ErrorHandler.Error($"[IAP] Initialization failed: {e}");
            }
        }

        #endregion

        #region Purchases

        /// <summary>
        /// Tente d’acheter un produit donné
        /// </summary>
        public void BuyProduct(string productId, Action onSuccess)
        {
            if (!Initialized)
            {
                ErrorHandler.Warning("[IAP] Non initialisé.");
                return;
            }

            var productToBuy = GetProduct(productId);

            if (productToBuy == null)
            {
                ScreenManager.QuickMessage(
                    $"Produit invalide ou non disponible : {productId}\nIf the problem persists, please report the issue.");
                return;
            }

            m_OnPurchaseSuccess = onSuccess;

            // v5: use PurchaseProduct instead of InitiatePurchase
            m_StoreController.PurchaseProduct(productToBuy);
        }

        /// <summary>
        /// Restaure les achats sur iOS / macOS (v5 way)
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_IOS || UNITY_STANDALONE_OSX
            if (!Initialized)
                return;

            m_StoreController.RestoreTransactions((success, error) =>
            {
                Debug.Log($"[IAP] RestoreTransactions finished. Success: {success}, error: {error}");
            });
#endif
        }

        #endregion

        #region Product Management

        public Product GetProduct(EProduct product)
        {
            return GetProduct(product.ToString());
        }

        public Product GetProduct(string productId)
        {
            if (!Initialized)
                return null;

            // v5: use StoreController.GetProductById
            return m_StoreController.GetProductById(productId);
        }

        #endregion

        #region StoreController callbacks (v5 replacement for IStoreListener)

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAP] Products fetched: {products.Count}");
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            ErrorHandler.Error($"[IAP] Product fetch failed: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            // Called for restored purchases, etc.
            Debug.Log($"[IAP] Purchases fetched. Confirmed: {orders.ConfirmedOrders.Count}, Pending: {orders.PendingOrders.Count}");
            // If you have non-consumables/subscriptions, re-grant entitlements here.
        }

        /// <summary>
        /// v5 replacement for ProcessPurchase.
        /// Called both for new purchases and (optionally) pending ones.
        /// </summary>
        private void OnPurchasePending(PendingOrder order)
        {
            Debug.Log("[IAP] Purchase pending; granting content and confirming.");

            // Re-use your old ProcessPurchase logic here:
            m_OnPurchaseSuccess?.Invoke();
            m_OnPurchaseSuccess = null;

            // Required in v5: confirm the pending order when you're done
            m_StoreController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            // Order can be ConfirmedOrder or FailedOrder, but by this point
            // you've already run your success logic in OnPurchasePending.
            Debug.Log("[IAP] Purchase confirmed.");
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"[IAP] Purchase failed. Details: {failedOrder.Details}");
        }

        #endregion
    }
}
