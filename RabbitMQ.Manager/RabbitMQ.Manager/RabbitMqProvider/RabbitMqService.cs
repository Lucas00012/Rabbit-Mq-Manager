using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RabbitMQ.Manager.RabbitMqProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RabbitMQ.Manager.RabbitMqProvider
{
	public class RabbitMqService : IRabbitMqService
	{
		private const string EXCHANGE_TYPE = "topic";

		private readonly int _retryCount = 3;
		private readonly int _retryDelay = 100;
		private readonly IModel _channel;
		private readonly JsonSerializerSettings _defaultSerializerSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new List<JsonConverter> { new StringEnumConverter() },
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		};

		public RabbitMqService()
		{
			var factory = new ConnectionFactory() { HostName = "localhost" };
			var connection = factory.CreateConnection();

			_channel = connection.CreateModel();
		}

		public void Consume<T>(Func<T, Task> action, string exchangeName, string queueName, string routingKey, int workersNumber = 1)
		{
			queueName = $"{queueName}.Q";
			var queueNameError = $"{queueName}.Error"; 

			exchangeName = $"{exchangeName}.E";
			var exchangeNameError = $"{exchangeName}.Error";

			_channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

			_channel.ExchangeDeclare(exchange: exchangeName, type: EXCHANGE_TYPE, true, false, null);
			_channel.ExchangeDeclare(exchange: exchangeNameError, type: EXCHANGE_TYPE, true, false, null); //just for logging exceptions

			_channel.QueueDeclare(queueName, true, false, false, null);
			_channel.QueueDeclare(queueNameError, true, false, false, null); //just for logging exceptions

			_channel.QueueBind(queueName, exchangeName, routingKey);
			_channel.QueueBind(queueNameError, exchangeNameError, routingKey);

			for(var i = 0; i < workersNumber; i++)
			{
				var consumer = new EventingBasicConsumer(_channel);

				consumer.Received += (ch, ea) =>
				{
					T body;
					try
					{
						body = GetBody<T>(ea.Body.ToArray());
					}
					catch(Exception e)
					{
						ThreatException(e, ea, exchangeNameError, routingKey);
						return;
					}

					var result = action.Invoke(body);
					var retries = 0;

					while (result.IsFaulted)
					{
						if(retries == _retryCount)
						{
							ThreatException(result.Exception, ea, exchangeNameError, routingKey);
							return;
						}

						Thread.Sleep(_retryDelay);
						result = action.Invoke(body);
						retries++;
					}

					_channel.BasicAck(ea.DeliveryTag, false);
				};

				_channel.BasicConsume(queueName, false, consumer);
			}
		}

		public void Send<T>(T message, string exchangeName, string routingKey)
		{
			var properties = _channel.CreateBasicProperties();
			properties.Persistent = true;

			exchangeName = $"{exchangeName}.E";
			var exchangeNameError = $"{exchangeName}.Error";

			_channel.ExchangeDeclare(exchange: exchangeName, type: EXCHANGE_TYPE, true, false, null);
			_channel.ExchangeDeclare(exchange: exchangeNameError, type: EXCHANGE_TYPE, true, false, null); //just for logging

			var sendBytes = GetBytes(message);

			_channel.BasicPublish(exchangeName, routingKey, properties, sendBytes);
		}

		#region Utils
		private byte[] GetBytes(object message)
		{
			var stringContent = JsonConvert.SerializeObject(message, _defaultSerializerSettings);
			var sendBytes = Encoding.UTF8.GetBytes(stringContent);

			return sendBytes;
		}

		private T GetBody<T>(byte[] bytes)
		{
			var stringContent = Encoding.UTF8.GetString(bytes);
			var sendObject = JsonConvert.DeserializeObject<T>(stringContent);

			return sendObject;
		}

		private void ThreatException(Exception e, BasicDeliverEventArgs ea, string exchangeNameError, string routingKey)
		{
			var properties = _channel.CreateBasicProperties();
			properties.Persistent = true;
			properties.Headers = new Dictionary<string, object>
			{
				{ "Exception", e?.Message }
			};

			_channel.BasicPublish(exchangeNameError, routingKey, properties, ea.Body);
			_channel.BasicAck(ea.DeliveryTag, false);
		}
		#endregion
	}
}
