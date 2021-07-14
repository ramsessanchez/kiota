using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Abstractions
{
    public class RequestInfo
    {
        public Uri URI { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public IDictionary<string, object> QueryParameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Stream Content { get; set; }
        private Dictionary<string, IMiddlewareOption> _middlewareOptions = new Dictionary<string, IMiddlewareOption>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the middleware options for this request. Options are unique by type. If an option of the same type is added twice, the last one wins.
        /// </summary>
        public IEnumerable<IMiddlewareOption> MiddlewareOptions { get { return _middlewareOptions.Values; } }
        /// <summary>
        /// Adds a middleware option to the request.
        /// </summary>
        /// <param name="middlewareOption">The middleware option to add.</param>
        public void AddMiddlewareOptions(params IMiddlewareOption[] options) {
            if(!options?.Any() ?? false) throw new ArgumentNullException(nameof(options));
            foreach(var option in options.Where(x => x != null))
                if(!_middlewareOptions.TryAdd(option.GetType().FullName, option))
                    _middlewareOptions[option.GetType().FullName] = option;
        }
        /// <summary>
        /// Removes given middleware options from the current request.
        /// </summary>
        /// <param name="options">Middleware options to remove.</param>
        public void RemoveMiddlewareOptions(params IMiddlewareOption[] options) {
            if(!options?.Any() ?? false) throw new ArgumentNullException(nameof(options));
            foreach(var optionName in options.Where(x => x != null).Select(x => x.GetType().FullName))
                _middlewareOptions.Remove(optionName);
        }
        private const string binaryContentType = "application/octet-stream";
        private const string contentTypeHeader = "Content-Type";
        public void SetStreamContent(Stream content) {
            Content = content;
            Headers.Add(contentTypeHeader, binaryContentType);
        }
        public void SetContentFromParsable<T>(T item, IHttpCore coreService, string contentType) where T : IParsable {
            if(string.IsNullOrEmpty(contentType)) throw new ArgumentNullException(nameof(contentType));
            if(coreService == null) throw new ArgumentNullException(nameof(coreService));

            using var writer = coreService.SerializationWriterFactory.GetSerializationWriter(contentType);
            writer.WriteObjectValue(null, item);
            Headers.Add(contentTypeHeader, contentType);
            Content = writer.GetSerializedContent();
        }
    }
}
