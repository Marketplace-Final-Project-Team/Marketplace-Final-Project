// Small shared Chart.js wrappers reused across the admin/seller analytics pages.
// Every function is a no-op if the canvas isn't on the page or Chart.js didn't load.
window.MpCharts = (() => {
    function readData(canvas) {
        try {
            return JSON.parse(canvas.dataset.chart || "null");
        } catch {
            return null;
        }
    }

    function baseOptions(extra) {
        return Object.assign({
            responsive: true,
            maintainAspectRatio: false
        }, extra);
    }

    function doughnut(canvasId, { labels, data, colors }) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === "undefined") {
            return;
        }

        new Chart(canvas, {
            type: "doughnut",
            data: { labels, datasets: [{ data, backgroundColor: colors, borderWidth: 0 }] },
            options: baseOptions({
                cutout: "62%",
                plugins: { legend: { position: "bottom", labels: { boxWidth: 12, font: { size: 11 } } } }
            })
        });
    }

    function truncateLabel(value) {
        return value.length > 16 ? value.slice(0, 15) + "…" : value;
    }

    function bar(canvasId, { labels, data, label, color, horizontal }) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === "undefined") {
            return;
        }

        const categoryScale = { ticks: { autoSkip: false } };
        const valueScale = { beginAtZero: true, ticks: { precision: 0 } };

        if (horizontal) {
            categoryScale.ticks.callback = function (value) {
                return truncateLabel(String(this.getLabelForValue(value)));
            };
        }

        new Chart(canvas, {
            type: "bar",
            data: { labels, datasets: [{ label, data, backgroundColor: color, borderRadius: 6, maxBarThickness: 32 }] },
            options: baseOptions({
                indexAxis: horizontal ? "y" : "x",
                plugins: { legend: { display: false } },
                scales: horizontal
                    ? { x: valueScale, y: categoryScale }
                    : { x: categoryScale, y: valueScale }
            })
        });
    }

    function line(canvasId, { labels, data, label, color }) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === "undefined") {
            return;
        }

        new Chart(canvas, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label,
                    data,
                    borderColor: color,
                    backgroundColor: color + "22",
                    borderWidth: 2,
                    tension: 0.3,
                    pointRadius: 2,
                    fill: true
                }]
            },
            options: baseOptions({
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            })
        });
    }

    function combo(canvasId, { labels, revenue, orders, revenueLabel, ordersLabel }) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === "undefined") {
            return;
        }

        new Chart(canvas, {
            type: "bar",
            data: {
                labels,
                datasets: [
                    {
                        type: "line",
                        label: revenueLabel || __t("Виручка, ₴"),
                        data: revenue,
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
                        label: ordersLabel || __t("Замовлення"),
                        data: orders,
                        backgroundColor: "rgba(66, 169, 139, 0.55)",
                        yAxisID: "orders",
                        borderRadius: 4,
                        maxBarThickness: 22
                    }
                ]
            },
            options: baseOptions({
                interaction: { mode: "index", intersect: false },
                scales: {
                    revenue: { type: "linear", position: "left", beginAtZero: true, ticks: { precision: 0 } },
                    orders: { type: "linear", position: "right", beginAtZero: true, ticks: { precision: 0 }, grid: { drawOnChartArea: false } }
                },
                plugins: { legend: { position: "bottom" } }
            })
        });
    }

    return { readData, doughnut, bar, line, combo };
})();
