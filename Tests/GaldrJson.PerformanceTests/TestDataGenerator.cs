using System;
using System.Collections.Generic;

namespace GaldrJson.PerformanceTests;

[GaldrJsonSerializable]
public class ProductCatalog
{
    public int CatalogId { get; set; }
    public string CatalogName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public List<Product> Products { get; set; }
    public List<Order> Orders { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public ProductDetails Details { get; set; }
}

public class ProductDetails
{
    public string Manufacturer { get; set; }
    public string SKU { get; set; }
    public double Weight { get; set; }
    public Dimensions Dimensions { get; set; }
}

public class Dimensions
{
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Unit { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; }
    public Address ShippingAddress { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public static class TestDataGenerator
{
    private static readonly Random _random = new Random(69); // Fixed seed for consistency

    private static readonly string[] _productNames = new[]
    {
        "Wireless Mouse", "Mechanical Keyboard", "USB Cable", "HDMI Adapter",
        "Laptop Stand", "Monitor Arm", "Desk Pad", "Webcam", "Headphones",
        "Microphone", "External SSD", "USB Hub", "Cable Organizer", "Phone Stand"
    };

    private static readonly string[] _categories = new[]
    {
        "Electronics", "Accessories", "Cables", "Peripherals", "Storage"
    };

    private static readonly string[] _manufacturers = new[]
    {
        "TechCorp", "DeviceWorks", "AccessoryPlus", "ElectroMax", "GadgetPro"
    };

    private static readonly string[] _firstNames = new[]
    {
        "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda"
    };

    private static readonly string[] _lastNames = new[]
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis"
    };

    public static ProductCatalog GenerateCatalog(int productCount = 50, int orderCount = 30)
    {
        var catalog = new ProductCatalog
        {
            CatalogId = 1001,
            CatalogName = "Electronics & Accessories Catalog 2024",
            CreatedDate = new DateTime(2024, 1, 1, 10, 30, 0, DateTimeKind.Utc),
            IsActive = true,
            Products = new List<Product>(),
            Orders = new List<Order>(),
            Metadata = new Dictionary<string, string>
            {
                { "version", "2.0" },
                { "region", "North America" },
                { "currency", "USD" },
                { "language", "en-US" }
            }
        };

        // Generate products
        for (int i = 0; i < productCount; i++)
        {
            catalog.Products.Add(GenerateProduct(i + 1));
        }

        // Generate orders
        for (int i = 0; i < orderCount; i++)
        {
            catalog.Orders.Add(GenerateOrder(i + 1, catalog.Products));
        }

        return catalog;
    }

    private static Product GenerateProduct(int id)
    {
        return new Product
        {
            ProductId = id,
            Name = _productNames[_random.Next(_productNames.Length)],
            Description = $"High-quality {_productNames[_random.Next(_productNames.Length)].ToLower()} with advanced features and excellent build quality.",
            Price = (decimal)(_random.NextDouble() * 200 + 10),
            StockQuantity = _random.Next(0, 500),
            Category = _categories[_random.Next(_categories.Length)],
            Tags = new List<string>
            {
                "new-arrival",
                _random.Next(2) == 0 ? "on-sale" : "trending",
                "bestseller"
            },
            Details = new ProductDetails
            {
                Manufacturer = _manufacturers[_random.Next(_manufacturers.Length)],
                SKU = $"SKU-{id:D6}",
                Weight = _random.NextDouble() * 2 + 0.1,
                Dimensions = new Dimensions
                {
                    Length = _random.NextDouble() * 20 + 5,
                    Width = _random.NextDouble() * 15 + 3,
                    Height = _random.NextDouble() * 10 + 2,
                    Unit = "cm"
                }
            }
        };
    }

    private static Order GenerateOrder(int id, List<Product> products)
    {
        var itemCount = _random.Next(1, 6);
        var items = new List<OrderItem>();
        decimal total = 0;

        for (int i = 0; i < itemCount; i++)
        {
            var product = products[_random.Next(products.Count)];
            var quantity = _random.Next(1, 4);
            var subtotal = product.Price * quantity;
            total += subtotal;

            items.Add(new OrderItem
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = product.Price,
                Subtotal = subtotal
            });
        }

        return new Order
        {
            OrderId = id,
            OrderDate = DateTime.UtcNow.AddDays(-_random.Next(0, 90)),
            CustomerName = $"{_firstNames[_random.Next(_firstNames.Length)]} {_lastNames[_random.Next(_lastNames.Length)]}",
            CustomerEmail = $"customer{id}@example.com",
            TotalAmount = total,
            Status = (OrderStatus)_random.Next(0, 5),
            Items = items,
            ShippingAddress = new Address
            {
                Street = $"{_random.Next(100, 9999)} Main Street",
                City = "Springfield",
                State = "IL",
                PostalCode = $"{_random.Next(10000, 99999)}",
                Country = "USA"
            }
        };
    }
}
