(() => {
    if (typeof Chart === "undefined" || !window.MpCharts) {
        return;
    }

    const canvas = document.getElementById("financeTrendChart");
    if (!canvas) {
        return;
    }

    const points = MpCharts.readData(canvas) || [];
    if (points.length === 0) {
        return;
    }

    MpCharts.combo("financeTrendChart", {
        labels: points.map((p) => p.date),
        revenue: points.map((p) => p.revenue),
        orders: points.map((p) => p.orders)
    });
})();
