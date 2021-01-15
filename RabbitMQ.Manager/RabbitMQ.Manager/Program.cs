using RabbitMQ.Manager.RabbitMqProvider;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Manager
{
	class Program
	{

		static void Main(string[] args)
		{
			IRabbitMqService rabbitMqService = new RabbitMqService();

			rabbitMqService.Send("Hello Word!", exchangeName: "MyExchange", routingKey: "StringContent");
			rabbitMqService.Consume<string>((message) =>
			{
				Console.WriteLine(message);

				return Task.CompletedTask;
			}, exchangeName: "MyExchange", queueName: "PrintHelloWord", routingKey: "StringContent");

			rabbitMqService.Send(5, exchangeName: "MyExchange", routingKey: "IntContent");
			rabbitMqService.Consume<int>((message) =>
			{
				Console.WriteLine($"Fat({message}) is equal to: {Fat(message)}");

				return Task.CompletedTask;
			}, exchangeName: "MyExchange", queueName: "PrintNumbers", routingKey: "IntContent");

			rabbitMqService.Send(new Student("Lucas", 19), exchangeName: "MyExchange", routingKey: "FetchStudents");
			rabbitMqService.Consume<Student>((message) =>
			{
				Console.WriteLine("--- Fetching Student ---");
				Thread.Sleep(5000);

				Console.WriteLine($"name: {message.Name}, age: {message.Age}");

				return Task.CompletedTask;
			}, exchangeName: "MyExchange", queueName: "HappySchool", routingKey: "FetchStudents");

			//serialize error
			rabbitMqService.Send(500, exchangeName: "MyExchange", routingKey: "FetchStudents");
			rabbitMqService.Send(150, exchangeName: "MyExchange", routingKey: "FetchStudents");

			Console.ReadKey();
		}

		static int Fat(int n)
		{
			if (n == 1 || n == 0) return n;

			return n * Fat(n - 1);
		}
	}

	class Student
	{
		public string Name { get; set; }
		public int Age { get; set; }

		public Student(string name, int age)
		{
			Name = name;
			Age = age;
		}
	}
}
