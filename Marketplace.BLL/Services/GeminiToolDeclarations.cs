namespace Marketplace.BLL.Services
{
    public static class GeminiToolDeclarations
    {
        public static object[] GetAll() => new object[]
        {
            // Customer
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
                name = "get_categories",
                description = "Повертає список усіх доступних категорій товарів у базі (англійською). Виклич перед search_products чи get_discounts, якщо не впевнений у точній назві категорії",
                parameters = new
                {
                    type = "object",
                    properties = new { }
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
            },

            // Business
            new
            {
                name = "get_my_businesses",
                description = "Повертає список магазинів (бізнесів), власником або учасником яких є поточний користувач, з їх ID та назвами. Виклич цю функцію ПЕРШОЮ для будь-якого бізнес-питання, якщо businessId невідомий. Якщо магазин один — автоматично використовуй його ID без уточнення. Якщо декілька — запитай користувача, про який саме магазин йдеться",
                parameters = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_sales_summary",
                description = "Повертає підсумок продажів бізнесу за період: загальний дохід, кількість замовлень, середній чек",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        startDate = new { type = "string", description = "Початкова дата періоду у форматі ISO 8601 (YYYY-MM-DD). Необов'язково — якщо не вказано, береться останні 30 днів" },
                        endDate = new { type = "string", description = "Кінцева дата періоду у форматі ISO 8601 (YYYY-MM-DD). Необов'язково" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_products_sales_stats",
                description = "Повертає статистику продажів по товарах бізнесу за період — найкращі або найгірші за продажами",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        startDate = new { type = "string", description = "Початкова дата періоду, ISO 8601. Необов'язково" },
                        endDate = new { type = "string", description = "Кінцева дата періоду, ISO 8601. Необов'язково" },
                        sortBy = new { type = "string", description = "'best' для найбільш продаваних товарів, 'worst' для найменш продаваних" },
                        limit = new { type = "integer", description = "Скільки товарів повернути. За замовчуванням 5" }
                    },
                    required = new[] { "businessId", "sortBy" }
                }
            },
            new
            {
                name = "get_business_balance",
                description = "Повертає поточний баланс магазину",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_recent_reviews",
                description = "Повертає останні відгуки на всі товари бізнесу",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        limit = new { type = "integer", description = "Скільки відгуків повернути. За замовчуванням 5" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_out_of_stock_products",
                description = "Повертає товари бізнесу, яких зараз немає в наявності",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_low_stock_products",
                description = "Повертає товари бізнесу, кількість яких на складі нижче вказаного порогу (але більше нуля)",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        threshold = new { type = "integer", description = "Поріг кількості. За замовчуванням 5" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_average_rating_for_product",
                description = "Повертає середній рейтинг конкретного товару бізнесу",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        productId = new { type = "string", description = "GUID товару" }
                    },
                    required = new[] { "businessId", "productId" }
                }
            },
            new
            {
                name = "get_reviews_for_product",
                description = "Повертає останні відгуки на конкретний товар бізнесу",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" },
                        productId = new { type = "string", description = "GUID товару" },
                        limit = new { type = "integer", description = "Скільки відгуків повернути. За замовчуванням 5" }
                    },
                    required = new[] { "businessId", "productId" }
                }
            },
            new
            {
                name = "get_business_members",
                description = "Повертає список учасників команди бізнесу та їх ролі",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        businessId = new { type = "string", description = "GUID бізнесу" }
                    },
                    required = new[] { "businessId" }
                }
            },
            new
            {
                name = "get_bulk_import_instructions",
                description = "Повертає покрокову інструкцію, як масово додати товари на склад через таблицю",
                parameters = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_withdrawal_instructions",
                description = "Повертає покрокову інструкцію, як власнику магазину подати заявку на виведення коштів",
                parameters = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_moderation_policy_summary",
                description = "Повертає короткий опис правил модерації товарів на маркетплейсі",
                parameters = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_discount_setup_instructions",
                description = "Повертає покрокову інструкцію, як встановити знижку на товар",
                parameters = new
                {
                    type = "object",
                    properties = new { }
                }
            }
        };
    }
}