(() => {
    if (typeof Chart === "undefined" || !window.MpCharts) {
        return;
    }

    const statusCanvas = document.getElementById("orderStatusChart");
    if (statusCanvas) {
        const items = (MpCharts.readData(statusCanvas) || []).filter((s) => s.count > 0);
        if (items.length > 0) {
            MpCharts.doughnut("orderStatusChart", {
                labels: items.map((s) => s.label),
                data: items.map((s) => s.count),
                colors: ["#667085", "#3157d7", "#8a4fd6", "#22a06b", "#d92d20"]
            });
        }
    }

    const ratingCanvas = document.getElementById("ratingChart");
    if (ratingCanvas) {
        const distribution = MpCharts.readData(ratingCanvas) || [];
        if (distribution.length === 5) {
            MpCharts.bar("ratingChart", {
                labels: ["1 ★", "2 ★", "3 ★", "4 ★", "5 ★"],
                data: distribution,
                label: "Відгуків",
                color: "#d99a16"
            });
        }
    }
})();
