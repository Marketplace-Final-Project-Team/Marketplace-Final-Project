(() => {
    const canvas = document.getElementById("revenueChart");
    if (!canvas || typeof Chart === "undefined") {
        return;
    }

    let points = [];
    try {
        points = JSON.parse(canvas.dataset.chart || "[]");
    } catch {
        return;
    }

    if (points.length === 0) {
        return;
    }

    new Chart(canvas, {
        type: "bar",
        data: {
            labels: points.map((p) => p.date),
            datasets: [
                {
                    type: "line",
                    label: "Виручка, ₴",
                    data: points.map((p) => p.revenue),
                    borderColor: "#3157d7",
                    backgroundColor: "rgba(49, 87, 215, 0.12)",
                    borderWidth: 2,
                    tension: 0.3,
                    yAxisID: "revenue",
                    pointRadius: 2,
                    fill: true
                },
                {
                    type: "bar",
                    label: "Замовлення",
                    data: points.map((p) => p.orders),
                    backgroundColor: "rgba(66, 169, 139, 0.55)",
                    yAxisID: "orders",
                    borderRadius: 4,
                    maxBarThickness: 22
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                revenue: {
                    type: "linear",
                    position: "left",
                    beginAtZero: true,
                    ticks: { precision: 0 }
                },
                orders: {
                    type: "linear",
                    position: "right",
                    beginAtZero: true,
                    ticks: { precision: 0 },
                    grid: { drawOnChartArea: false }
                }
            },
            plugins: {
                legend: { position: "bottom" }
            }
        }
    });
})();
