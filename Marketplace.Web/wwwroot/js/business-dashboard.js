(() => {
    if (typeof Chart === "undefined" || !window.MpCharts) {
        return;
    }

    const revenueCanvas = document.getElementById("revenueChart");
    if (revenueCanvas) {
        const points = MpCharts.readData(revenueCanvas) || [];
        if (points.length > 0) {
            MpCharts.combo("revenueChart", {
                labels: points.map((p) => p.date),
                revenue: points.map((p) => p.revenue),
                orders: points.map((p) => p.orders)
            });

            const aovCanvas = document.getElementById("aovChart");
            if (aovCanvas) {
                MpCharts.line("aovChart", {
                    labels: points.map((p) => p.date),
                    data: points.map((p) => (p.orders > 0 ? Math.round((p.revenue / p.orders) * 100) / 100 : 0)),
                    label: "Середній чек, ₴",
                    color: "#8a4fd6"
                });
            }
        }
    }

    const statusCanvas = document.getElementById("productStatusChart");
    if (statusCanvas) {
        const d = MpCharts.readData(statusCanvas);
        if (d) {
            MpCharts.doughnut("productStatusChart", {
                labels: ["Активні", "На модерації", "Немає в наявності", "Мало на складі"],
                data: [d.active, d.pending, d.outOfStock, d.lowStock],
                colors: ["#22a06b", "#d99a16", "#d92d20", "#f59e0b"]
            });
        }
    }

    const topProductsCanvas = document.getElementById("topProductsChart");
    if (topProductsCanvas) {
        const items = MpCharts.readData(topProductsCanvas) || [];
        if (items.length > 0) {
            MpCharts.bar("topProductsChart", {
                labels: items.map((p) => p.name),
                data: items.map((p) => p.revenue),
                label: "Виручка, ₴",
                color: "#3157d7",
                horizontal: true
            });
        }
    }

    const trafficCanvas = document.getElementById("trafficChart");
    if (trafficCanvas) {
        const d = MpCharts.readData(trafficCanvas);
        if (d) {
            MpCharts.bar("trafficChart", {
                labels: ["Перегляди", "У кошик", "У побажання", "Замовлення"],
                data: [d.views, d.cartAdds, d.wishlistAdds, d.orders],
                label: "Дії",
                color: "#42a98b"
            });
        }
    }

    const categoryCanvas = document.getElementById("categoryChart");
    if (categoryCanvas) {
        const items = MpCharts.readData(categoryCanvas) || [];
        if (items.length > 0) {
            MpCharts.doughnut("categoryChart", {
                labels: items.map((c) => c.name),
                data: items.map((c) => c.revenue),
                colors: items.map((c) => c.color)
            });
        }
    }
})();
