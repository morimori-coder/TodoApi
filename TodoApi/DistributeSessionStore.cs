using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using TodoApi.Models;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace TodoApi
{
    public class DistributeSessionStore : ITicketStore
    {
        public DistributeSessionStore(IDistributedCache distributedCache) 
        {
            _distributedCache= distributedCache;
        }
        private readonly IDistributedCache _distributedCache;

        public async Task<string> StoreAsync(AuthenticationTicket ticket, TodoItem todoItem)
        {
            // Serialize the response
            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(todoItem);
            var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectToCache);

            // GuidでTodoItemを一意に判別したい
            var key = Guid.NewGuid().ToString();
            await _distributedCache.SetStringAsync(key, jsonToDeserialize, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10) });
            return key;
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket) 
        {
            var serializedTicket = TicketSerializer.Default.Serialize(ticket);

            _distributedCache.Set(key, serializedTicket);
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key) 
        {
            // Find cached item
            byte[] objectFromCache = await _distributedCache.GetAsync(key);

            if (objectFromCache != null) 
            {
                // Deserialize it
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectFromCache);
                var cachedResult = JsonSerializer.Deserialize<TodoItem>(jsonToDeserialize);
                if (cachedResult != null)
                {
                    //HttpContext.Response.Cookies.Append("TodoItem", jsonToDeserialize);
                    // If found, then return it
                    return cachedResult;
                }
            }

            return null;
        }

        public async Task RemoveAsync(string key)
        {
            _distributedCache.Remove(key);
        }
    }
}
