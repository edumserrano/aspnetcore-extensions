﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNet.Sdk.Extensions.Testing.HttpMocking.HttpMessageHandlers.ResponseMocking
{
    public class HttpResponseMessageMockBuilder
    {
        private readonly HttpResponseMessageMockPredicateDelegate _defaultPredicate = (httpRequestMessage, cancellationToken) => Task.FromResult(true);
        private HttpResponseMessageMockPredicateDelegate? _predicateAsync;
        private HttpResponseMessageMockHandlerDelegate? _handlerAsync;

        public HttpResponseMessageMockBuilder Where(Func<HttpRequestMessage, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            // convert to 'async' predicate
            return Where((httpRequestMessage, cancellationToken) => Task.FromResult(predicate(httpRequestMessage)));
        }

        public HttpResponseMessageMockBuilder Where(HttpResponseMessageMockPredicateDelegate predicate)
        {
            if (_predicateAsync != null)
            {
                throw new HttpResponseMessageMockBuilderException("Where condition already configured.");
            }
            _predicateAsync = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public HttpResponseMessageMockBuilder RespondWith(HttpResponseMessage httpResponseMessage)
        {
            return RespondWith(httpRequestMessage => httpResponseMessage);
        }

        public HttpResponseMessageMockBuilder RespondWith(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            // convert to 'async' handler
            return RespondWith((httpRequestMessage, cancellationToken) => Task.FromResult(handler(httpRequestMessage)));
        }

        public HttpResponseMessageMockBuilder RespondWith(HttpResponseMessageMockHandlerDelegate handler)
        {
            if (_handlerAsync != null)
            {
                throw new HttpResponseMessageMockBuilderException("RespondWith already configured.");
            }
            _handlerAsync = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        public HttpResponseMessageMock Build()
        {
            // predicate is not mandatory. The default predicate represents an always apply condition.
            _predicateAsync ??= _defaultPredicate;
            if (_handlerAsync is null)
            {
                throw new HttpResponseMessageMockBuilderException("HttpResponseMessage not configured for HttpResponseMock. Use RespondWith to configure it.");
            }

            return new HttpResponseMessageMock(_predicateAsync, _handlerAsync);
        }
    }
}