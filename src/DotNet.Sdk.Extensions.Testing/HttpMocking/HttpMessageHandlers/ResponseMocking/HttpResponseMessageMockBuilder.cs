﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Sdk.Extensions.Testing.HttpMocking.OutOfProcess.ResponseMocking;

namespace DotNet.Sdk.Extensions.Testing.HttpMocking.HttpMessageHandlers.ResponseMocking
{
    /// <summary>
    /// Defines methods to configure and build an <see cref="HttpResponseMessageMock"/>
    /// </summary>
    public class HttpResponseMessageMockBuilder
    {
        private readonly HttpResponseMessageMockPredicateDelegate _defaultPredicate = (httpRequestMessage, cancellationToken) => Task.FromResult(true);
        private HttpResponseMessageMockPredicateDelegate? _predicateAsync;
        private HttpResponseMessageMockHandlerDelegate? _handlerAsync;

        /// <summary>
        /// Define the condition for the <see cref="HttpResponseMessageMock"/> to be executed.
        /// </summary>
        /// <param name="predicate">The predicate that determines if the <see cref="HttpResponseMessageMock"/> is executed or not.</param>
        /// <returns>The <see cref="HttpResponseMessageMockBuilder"/> for chaining.</returns>
        public HttpResponseMessageMockBuilder Where(Func<HttpRequestMessage, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            // convert to 'async' predicate
            return Where((httpRequestMessage, cancellationToken) => Task.FromResult(predicate(httpRequestMessage)));
        }

        /// <summary>
        /// Define the condition for the <see cref="HttpResponseMessageMock"/> to be executed.
        /// </summary>
        /// <param name="predicate">The predicate that determines if the <see cref="HttpResponseMessageMock"/> is executed or not.</param>
        /// <returns>The <see cref="HttpResponseMessageMockBuilder"/> for chaining.</returns>
        public HttpResponseMessageMockBuilder Where(HttpResponseMessageMockPredicateDelegate predicate)
        {
            if (_predicateAsync != null)
            {
                throw new HttpResponseMessageMockBuilderException($"{nameof(HttpResponseMessageMockBuilder)}.{nameof(Where)} condition already configured.");
            }
            _predicateAsync = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        /// <summary>
        /// Configure the <see cref="HttpResponseMessage"/> produced by the mock.
        /// </summary>
        /// <param name="httpResponseMessage">The <see cref="HttpResponseMessage"/> that the mock returns when executed.</param>
        /// <returns>The <see cref="HttpResponseMessageMockBuilder"/> for chaining.</returns>
        public HttpResponseMessageMockBuilder RespondWith(HttpResponseMessage httpResponseMessage)
        {
            return RespondWith(httpRequestMessage => httpResponseMessage);
        }
        
        /// <summary>
        /// Configure the <see cref="HttpResponseMessage"/> produced by the mock.
        /// </summary>
        /// <param name="handler">Function to configure the <see cref="HttpResponseMessage"/> produced by the mock.</param>
        /// <returns>The <see cref="HttpResponseMessageMockBuilder"/> for chaining.</returns>
        public HttpResponseMessageMockBuilder RespondWith(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            // convert to 'async' handler
            return RespondWith((httpRequestMessage, cancellationToken) => Task.FromResult(handler(httpRequestMessage)));
        }

        /// <summary>
        /// Configure the <see cref="HttpResponseMessage"/> produced by the mock.
        /// </summary>
        /// <param name="handler">Function to configure the <see cref="HttpResponseMessage"/> produced by the mock.</param>
        /// <returns>The <see cref="HttpResponseMessageMockBuilder"/> for chaining.</returns>
        public HttpResponseMessageMockBuilder RespondWith(HttpResponseMessageMockHandlerDelegate handler)
        {
            if (_handlerAsync != null)
            {
                throw new HttpResponseMessageMockBuilderException($"{nameof(HttpResponseMessageMockBuilder)}.{nameof(RespondWith)} already configured.");
            }
            _handlerAsync = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        /// <summary>
        /// Builds an instance of <see cref="HttpResponseMessageMock"/>.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessageMock"/> instance.</returns>
        public HttpResponseMessageMock Build()
        {
            // predicate is not mandatory. The default predicate represents an always apply condition.
            _predicateAsync ??= _defaultPredicate;
            if (_handlerAsync is null)
            {
                throw new HttpResponseMessageMockBuilderException($"{nameof(HttpResponseMessage)} not configured for {nameof(IHttpResponseMock)}. Use {nameof(HttpResponseMessageMockBuilder)}.{nameof(RespondWith)} to configure it.");
            }

            return new HttpResponseMessageMock(_predicateAsync, _handlerAsync);
        }
    }
}
