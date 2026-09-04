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
                revenueLabel: "Товарооборот, ₴",
                ordersLabel: "Продажів"
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
                label: "Товарооборот, ₴",
                color: "#3157d7",
                horizontal: true
            });
        }
    }
})();
