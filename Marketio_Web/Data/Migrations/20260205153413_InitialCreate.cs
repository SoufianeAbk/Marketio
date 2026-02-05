using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Marketio_Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Price", "Stock", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5382), "Krachtige laptop met 16GB RAM en 512GB SSD", "/images/laptop.jpg", true, "Laptop Dell XPS 15", 1299.99m, 15, null },
                    { 2, 1, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5392), "Nieuwste iPhone met A17 Pro chip", "/images/iphone.jpg", true, "iPhone 15 Pro", 1099.00m, 25, null },
                    { 3, 1, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5396), "Crystal UHD 4K Smart TV", "/images/tv.jpg", true, "Samsung 4K TV 55\"", 699.99m, 10, null },
                    { 4, 1, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5399), "Noise cancelling koptelefoon", "/images/headphones.jpg", true, "Sony WH-1000XM5", 349.99m, 30, null },
                    { 5, 1, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5402), "10.9-inch Liquid Retina display", "/images/ipad.jpg", true, "iPad Air", 649.00m, 20, null },
                    { 6, 2, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5408), "Comfortabele sportschoenen", "/images/sneakers.jpg", true, "Nike Air Max Sneakers", 129.99m, 50, null },
                    { 7, 2, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5411), "Klassieke straight fit jeans", "/images/jeans.jpg", true, "Levi's 501 Jeans", 89.99m, 40, null },
                    { 8, 2, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5414), "Warme hoodie met logo", "/images/hoodie.jpg", true, "Adidas Hoodie", 59.99m, 35, null },
                    { 9, 2, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5417), "Katoenen polo shirt", "/images/polo.jpg", true, "Tommy Hilfiger Polo", 69.99m, 45, null },
                    { 10, 2, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5473), "Waterdichte winterjas", "/images/jacket.jpg", true, "North Face Winterjas", 249.99m, 20, null },
                    { 11, 3, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5477), "Handbook of Agile Software Craftsmanship", "/images/cleancode.jpg", true, "Clean Code - Robert Martin", 39.99m, 60, null },
                    { 12, 3, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5480), "From Journeyman to Master", "/images/pragmatic.jpg", true, "The Pragmatic Programmer", 44.99m, 55, null },
                    { 13, 3, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5483), "Elements of Reusable Object-Oriented Software", "/images/patterns.jpg", true, "Design Patterns", 49.99m, 40, null },
                    { 14, 3, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5486), "Complete serie van 7 boeken", "/images/harrypotter.jpg", true, "Harry Potter Box Set", 89.99m, 25, null },
                    { 15, 3, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5489), "How Constant Innovation Creates Successful Businesses", "/images/leanstartup.jpg", true, "The Lean Startup", 29.99m, 70, null },
                    { 16, 4, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5491), "Draadloze stofzuiger met laser", "/images/vacuum.jpg", true, "Dyson V15 Stofzuiger", 599.99m, 15, null },
                    { 17, 4, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5494), "Hetelucht friteuse 7.3L", "/images/airfryer.jpg", true, "Philips Airfryer XXL", 249.99m, 20, null },
                    { 18, 4, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5499), "Verstelbaar bureau 160x80cm", "/images/desk.jpg", true, "IKEA Bureau BEKANT", 349.00m, 12, null },
                    { 19, 4, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5502), "Koffiemachine met melkopschuimer", "/images/nespresso.jpg", true, "Nespresso Machine", 199.99m, 30, null },
                    { 20, 4, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5505), "Aluminium tafel met 6 stoelen", "/images/garden.jpg", true, "Tuinset 6 persoons", 799.99m, 8, null },
                    { 21, 5, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5508), "Extra dikke yoga mat 6mm", "/images/yogamat.jpg", true, "Yoga Mat Premium", 39.99m, 50, null },
                    { 22, 5, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5512), "Verstelbare dumbbell set", "/images/dumbbells.jpg", true, "Dumbbells Set 20kg", 149.99m, 25, null },
                    { 23, 5, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5515), "GPS hardloop smartwatch", "/images/garmin.jpg", true, "Garmin Forerunner 265", 449.99m, 18, null },
                    { 24, 5, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5518), "Officiële wedstrijdbal", "/images/football.jpg", true, "Voetbal Nike Strike", 29.99m, 40, null },
                    { 25, 5, new DateTime(2026, 2, 5, 15, 34, 13, 49, DateTimeKind.Utc).AddTicks(5521), "Verstelbare fitnessbank", "/images/bench.jpg", true, "Fitness Bench", 199.99m, 15, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
