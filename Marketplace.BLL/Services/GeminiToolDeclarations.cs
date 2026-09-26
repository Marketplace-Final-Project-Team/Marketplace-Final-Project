namespace Marketplace.BLL.Services
{
    public static class GeminiToolDeclarations
    {
        public static object[] GetAll() => new object[]
        {
            new
            {
                name = "search_products",
                description = "Шукає товари в каталозі за ключовими словами, категорією, продавцем, рейтингом і ціною",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        searchTerm = new { type = "string", description = "Пошуковий текст — частина назви або опису товару" },
                        categoryName = new { type = "string", description = "Точна назва категорії з бази (англійською). Якщо не впевнений — спочатку виклич get_categories" },
                        businessName = new { type = "string", description = "Назва продавця (магазину)" },
                        minRating = new { type = "number", description = "Мінімальний середній рейтинг товару, від 0 до 5" },
                        minPrice = new { type = "number", description = "Мінімальна ціна" },
                        maxPrice = new { type = "number", description = "Максимальна ціна" },
                        limit = new { type = "integer", description = "Скільки товарів повернути. За замовчуванням 10" }
                    }
                }
            },
            new
            {
                name = "get_product_details",
                description = "Повертає повну інформацію про один товар за його ID, включно з активними знижками",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару" }
                    },
                    required = new[] { "productId" }
                }
            },
            new
            {
                name = "compare_products",
                description = "Порівнює від 2 до 5 товарів за їх характеристиками (ціна, рейтинг, наявність тощо)",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productIds = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Масив GUID товарів для порівняння, мінімум 2"
                        }
                    },
                    required = new[] { "productIds" }
                }
            },
            new
            {
                name = "get_product_reviews",
                description = "Повертає останні відгуки про товар",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару" },
                        limit = new { type = "integer", description = "Скільки відгуків повернути. За замовчуванням 10" }
                    },
                    required = new[] { "productId" }
                }
            },
            new
            {
                name = "get_similar_products",
                description = "Повертає товари, схожі на вказаний, за категорією та ціновим діапазоном",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару, до якого шукаємо схожі" },
                        limit = new { type = "integer", description = "Скільки товарів повернути. За замовчуванням 5" }
                    },
                    required = new[] { "productId" }
                }
            },
            new
            {
                name = "check_availability",
                description = "Перевіряє, чи товар існує і чи є в наявності, включно з кількістю на складі",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару" }
                    },
                    required = new[] { "productId" }
                }
            },
            new
            {
                name = "get_discounts",
                description = "Повертає активні знижки, опційно відфільтровані за категорією",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        categoryName = new { type = "string", description = "Точна назва категорії з бази (англійською). Необов'язково" },
                        limit = new { type = "integer", description = "Скільки знижок повернути. За замовчуванням 10" }
                    }
                }
            },
            new
            {
                name = "add_to_cart",
                description = "Додає товар у кошик поточного користувача. Перед викликом обов'язково запитай підтвердження у користувача",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару" },
                        quantity = new { type = "integer", description = "Кількість, за замовчуванням 1" }
                    },
                    required = new[] { "productId" }
                }
            },
            new
            {
                name = "remove_from_cart",
                description = "Видаляє товар з кошика поточного користувача. Перед викликом обов'язково запитай підтвердження у користувача",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new { type = "string", description = "GUID товару" }
                    },
                    required = new[] { "productId" }
                }
            }
        };
    }
}
