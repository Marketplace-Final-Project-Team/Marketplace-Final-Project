(() => {
    if (typeof Chart === "undefined" || !window.MpCharts) {
        return;
    }

    const turnoverCanvas = document.getElementById("turnoverChart");
    if (turnoverCanvas) {
        const points = MpCharts.readData(turnoverCanvas) || [];
        if (points.length > 0) {
            MpCharts.combo("turnoverChart", {
                labels: points.map((p) => p.label),
                revenue: points.map((p) => p.turnover),
                orders: points.map((p) => p.orders),
                revenueLabel: __t("Товарооборот, ₴"),
                ordersLabel: __t("Продажів")
            });
        }
    }

    const businessCanvas = document.getElementById("businessTurnoverChart");
    if (businessCanvas) {
        const items = MpCharts.readData(businessCanvas) || [];
        if (items.length > 0) {
            MpCharts.bar("businessTurnoverChart", {
                labels: items.map((b) => b.name),
                data: items.map((b) => b.turnover),
                label: __t("Товарооборот, ₴"),
                color: "#3157d7",
                horizontal: true
            });
        }
    }

    const categoryCanvas = document.getElementById("categoryTurnoverChart");
    if (categoryCanvas) {
        const items = MpCharts.readData(categoryCanvas) || [];
        if (items.length > 0) {
            MpCharts.doughnut("categoryTurnoverChart", {
                labels: items.map((c) => c.name),
                data: items.map((c) => c.turnover),
                colors: items.map((c) => c.color)
            });
        }
    }
})();
