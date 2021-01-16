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

			rabbitMqService.Send(new Student("Lucas", 19), exchangeName: "MyExchange", routingKey: "FetchStudents.Print");
			rabbitMqService.Consume<Student>((message) =>
			{
				Console.WriteLine("--- Logging Student ---");
				Console.WriteLine($"name: {message.Name}, age: {message.Age}");

				return Task.CompletedTask;
			}, exchangeName: "MyExchange", queueName: "HappySchool.Fetch", routingKey: "FetchStudents.Print");

			//using topic exchange feature (HappySchool.History will process the content of FetchStudents.Print and FetchStudents.Remove)
			rabbitMqService.Consume<Student>((message) =>
			{
				Console.WriteLine("--- The Student Has Being Processed ---");
				Console.WriteLine($"name: {message.Name}, age: {message.Age}");

				return Task.CompletedTask;
			}, exchangeName: "MyExchange", queueName: "HappySchool.History", routingKey: "FetchStudents.*");

			//retry attempts error (async in this case for not blocks the unique console thread)
			rabbitMqService.Send(new Student("Lucas", 19), exchangeName: "MyExchange", routingKey: "FetchStudents.Remove");
			rabbitMqService.Consume<Student>(async (message) =>
			{
				throw new Exception("The student doesnt exists");
			}, exchangeName: "MyExchange", queueName: "HappySchool.Remove", routingKey: "FetchStudents.Remove");
			
			//serialize error
			rabbitMqService.Send(500, exchangeName: "MyExchange", routingKey: "FetchStudents.Print");
			rabbitMqService.Send(150, exchangeName: "MyExchange", routingKey: "FetchStudents.Print");

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
