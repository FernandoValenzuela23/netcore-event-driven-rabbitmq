using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using UserService.Data;
using UserService.Entities;

namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserServiceContext _context;

        public UsersController(UserServiceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUser()
        {
            return await _context.User.ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Notification message
            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.ID,
                newname = user.Name
            });
            PublishToMessageQueue("user.update", integrationEventData);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.User.Add(user);
            await _context.SaveChangesAsync();

            // Notification message
            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.ID,
                name = user.Name
            });
            PublishToMessageQueue("user.add", integrationEventData);

            return CreatedAtAction("GetUser", new { id = user.ID }, user);
        }


        private void PublishToMessageQueue(string integrationEvent, string eventData)
        {
            // TODO: Reuse and close connections and channel, etc
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var body = Encoding.UTF8.GetBytes(eventData);
            channel.BasicPublish(exchange: "user",
                routingKey: integrationEvent,
                basicProperties: null,
                body: body);

        }


    }
}
