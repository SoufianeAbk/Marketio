using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketio_Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/headphones.jpg' WHERE \"Name\" = 'Wireless Headphones'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/laptop.jpg'     WHERE \"Name\" = 'USB-C Hub'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/polo.jpg'       WHERE \"Name\" = 'Cotton T-Shirt'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/jeans.jpg'      WHERE \"Name\" = 'Denim Jeans'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/cleancode.jpg'  WHERE \"Name\" = 'C# Programming Guide'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/pragmatic.jpg'  WHERE \"Name\" = 'Web Development Basics'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/desk.jpg'       WHERE \"Name\" = 'Plant Pot Set'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/vacuum.jpg'     WHERE \"Name\" = 'Garden Tool Set'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/sneakers.jpg'   WHERE \"Name\" = 'Running Shoes'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '/images/yogamat.jpg'    WHERE \"Name\" = 'Yoga Mat'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Wireless+Headphones' WHERE \"Name\" = 'Wireless Headphones'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=USB-C+Hub'           WHERE \"Name\" = 'USB-C Hub'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Cotton+T-Shirt'      WHERE \"Name\" = 'Cotton T-Shirt'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Denim+Jeans'         WHERE \"Name\" = 'Denim Jeans'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=CSharp+Guide'        WHERE \"Name\" = 'C# Programming Guide'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Web+Dev+Basics'      WHERE \"Name\" = 'Web Development Basics'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Plant+Pots'          WHERE \"Name\" = 'Plant Pot Set'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Garden+Tools'        WHERE \"Name\" = 'Garden Tool Set'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Running+Shoes'       WHERE \"Name\" = 'Running Shoes'");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = 'https://via.placeholder.com/300?text=Yoga+Mat'            WHERE \"Name\" = 'Yoga Mat'");
        }
    }
}
