using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Manager.RabbitMqProvider
{
	public interface IRabbitMqService
	{
		public void Consume<T>(Func<T, Task> action, string exchangeName, string queueName, string routingKey, int consumers = 1);
		public void Send<T>(T message, string exchangeName, string routingKey);
	}
}
