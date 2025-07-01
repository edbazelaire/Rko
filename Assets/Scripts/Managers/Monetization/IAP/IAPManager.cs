using Assets.Scripts.Managers;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;


namespace Managers.Monetization.IAP
{
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
    {
        #region Members

        public static IAPManager Instance;

        [Serializable]
        public class ProductInfo
        {
            public EProduct         Product;
            public ProductType      Type;
        }

        [Header("Products")]
        [SerializeField] private List<ProductInfo> m_ProductsInfo = new List<ProductInfo>();

        private IStoreController    m_StoreController;
        private IExtensionProvider  m_ExtensionProvider;

        private Action              m_OnPurchaseSuccess;
        public static bool Initialized => Instance != null && Instance.m_StoreController != null && Instance.m_ExtensionProvider != null;

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

        public static void Initialize()
        {
            if (Initialized)
                return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (ProductInfo product in Instance.m_ProductsInfo)
            {
                builder.AddProduct(product.Product.ToString(), product.Type);
            }

            UnityPurchasing.Initialize(Instance, builder);
        }


        #endregion


        #region Achats

        /// <summary>
        /// Tente d’acheter un produit donné
        /// </summary>
        public void BuyProduct(string productId, Action onSuccess)
        {
            if (! Initialized)
            {
                ErrorHandler.Warning("[IAP] Non initialisé.");
                return;
            }

            Product productToBuy = GetProduct(productId);

            if (productToBuy == null || !productToBuy.availableToPurchase)
            {
                ScreenManager.QuickMessage($"Produit invalide ou non disponible : {productId}\nIf the problem persists, please report the issue.");
                return;
            }

            m_OnPurchaseSuccess = onSuccess;
            m_StoreController.InitiatePurchase(productToBuy);
        }

        /// <summary>
        /// Restaure les achats sur iOS
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_IOS || UNITY_STANDALONE_OSX
            if (!Initialized)
                return;

            var apple = m_ExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions(result =>
            {
                Debug.Log($"[IAP] Restauration terminée : {result}");
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
            return m_StoreController.products.WithID(productId);
        }

        #endregion


        #region Callbacks IStoreListener

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            m_StoreController = controller;
            m_ExtensionProvider = extensions;

            Debug.Log("[IAP] Initialisation réussie.");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            ErrorHandler.Error($"[IAP] Échec de l'initialisation : {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            ErrorHandler.Error($"[IAP] Échec de l'initialisation : {error} - {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Debug.Log($"[IAP] Purchase success : {args.purchasedProduct.definition.id}");

            m_OnPurchaseSuccess?.Invoke();
            m_OnPurchaseSuccess = null;

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.Log($"[IAP] Purchase Failed : {product} - " + failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.Log($"[IAP] Purchase Failed : {product} - " + failureDescription.message);
            throw new NotImplementedException();
        }

        #endregion
    }
}