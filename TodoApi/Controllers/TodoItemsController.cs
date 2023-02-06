using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoContext _context;

        private IDistributedCache _distributedCache { get; }

        public TodoItemsController(TodoContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            if (_context.TodoItems == null)
            {
                return NotFound();
            }
            List<TodoItem> result = new List<TodoItem>();

            try
            {
                result = await _context.TodoItems.ToListAsync();
               
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(@"C:\source\repos\log\ControllerLog.log", ex.ToString());
                throw ex;
            }
            return result;

        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            // Find cached item
            byte[] objectFromCache = await _distributedCache.GetAsync(id.ToString());

            if (objectFromCache != null)
            {
                // Deserialize it
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectFromCache);
                var cachedResult = JsonSerializer.Deserialize<TodoItem>(jsonToDeserialize);
                if (cachedResult != null)
                {
                    HttpContext.Response.Cookies.Append("TodoItem", jsonToDeserialize);
                    // If found, then return it
                    return cachedResult;
                }
            }
            if (_context.TodoItems == null)
          {
              return NotFound();
          }
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }
            return todoItem;
        }

        // PUT: api/TodoItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            try 
            {
                if (_context.TodoItems == null)
                {
                    return Problem("Entity set 'TodoContext.TodoItems'  is null.");
                }


                // Serialize the response
                byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(todoItem);
                var jsonToDeserialize = System.Text.Encoding.UTF8.GetString(objectToCache);
                HttpContext.Response.Cookies.Append("TodoItem", jsonToDeserialize);

                // Cache it
                //await _distributedCache.SetAsync(todoItem.Id.ToString(), objectToCache);
                await _distributedCache.SetStringAsync(todoItem.Id.ToString(), jsonToDeserialize, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(9) });

                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();
            } catch (Exception ex) 
            {
                System.IO.File.WriteAllText(@"C:\source\repos\log\ControllerLog.log", ex.ToString());
                throw ex;
            }

            return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            if (_context.TodoItems == null)
            {
                return NotFound();
            }
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TodoItemExists(int id)
        {
            return (_context.TodoItems?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
