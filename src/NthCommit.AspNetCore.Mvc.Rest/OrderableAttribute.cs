﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NthCommit.AspNetCore.Mvc.Rest.Extensions;
using NthCommit.AspNetCore.Mvc.Rest.Ordering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NthCommit.AspNetCore.Mvc.Rest
{
    public class OrderableAttribute : Attribute, IActionFilter
    {
        private readonly IEnumerable<string> _allowedProperties;
        private readonly Type _type;

        public OrderableAttribute(Type type, params string[] allowedProperties)
        {
            _type = type;
            _allowedProperties = allowedProperties;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var restController = context.Controller as RestApiController;
            if (restController == null)
            {
                return;
            }

            OrderRequest request = null;
            var orderValue = context.HttpContext.Request.Query.FirstOrDefaultWithKey("order") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(orderValue))
            {
                request = new OrderRequest(new List<OrderDescriptor>());
            }
            else
            {
                var descriptors = orderValue
                    .Split(',')
                    .Select(o => o.Trim())
                    .Select(o =>
                    {
                        var descendingRequested = o.StartsWith("-");
                        var ascendingRequested = o.StartsWith("+");
                        var unsignedPropertyName = descendingRequested || ascendingRequested ? o.Substring(1) : o;
                        return new OrderDescriptor(GetPropertyName(unsignedPropertyName), !descendingRequested);
                    });

                if (descriptors.Any(a => a.PropertyName == null))
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                request = new OrderRequest(descriptors.ToList());
            }
            
            restController.OrderRequest = request;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private string GetPropertyName(string requestedPropertyName)
        {
            // TODO: Cache reflection
            var matchedPropertyName = _type
                .GetProperties()
                .Where(p => p.Name.ToLowerInvariant() == requestedPropertyName.ToLowerInvariant())
                .Select(p => p.Name)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(matchedPropertyName))
            {
                return null;
            }

            if (_allowedProperties.Count() > 0 &&
                !_allowedProperties.Any(p => p.ToLowerInvariant() == matchedPropertyName.ToLowerInvariant()))
            {
                return null;
            }

            return matchedPropertyName;
        }
    }
}
