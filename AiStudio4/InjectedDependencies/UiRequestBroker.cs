// AiStudio4/InjectedDependencies/UiRequestBroker.cs
using AiStudio4.InjectedDependencies.RequestHandlers;





namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Broker for handling UI requests from clients
    /// </summary>
    public class UiRequestBroker
    {
        private readonly UiRequestRouter _router;
        private readonly ClipboardImageRequestHandler _clipboardImageHandler;

        public UiRequestBroker(UiRequestRouter router, ClipboardImageRequestHandler clipboardImageHandler)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _clipboardImageHandler = clipboardImageHandler ?? throw new ArgumentNullException(nameof(clipboardImageHandler));
        }

        /// <summary>
        /// Handles clipboard image requests (API endpoint)
        /// </summary>
        public Task<string> HandleClipboardImageRequest(string clientId, string requestData)
        {
            return _clipboardImageHandler.HandleClipboardImageRequest(clientId, requestData);
        }

        /// <summary>
        /// Handles a request from a client
        /// </summary>
        public Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            return _router.RouteRequestAsync(clientId, requestType, requestData);
        }
    }
}
