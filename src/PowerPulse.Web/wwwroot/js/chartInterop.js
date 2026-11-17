// PowerPulse Chart.js Interop Helper
window.powerPulseCharts = {
    instances: {},

    destroy: function (canvasId) {
        if (this.instances[canvasId]) {
            this.instances[canvasId].destroy();
            delete this.instances[canvasId];
        }
    },

    renderLineChart: function (canvasId, labels, currentData, previousData, unit = 'kWh', title = 'Energy Consumption') {
        this.destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const datasets = [
            {
                label: 'Current Period',
                data: currentData,
                borderColor: '#1DA058',
                backgroundColor: 'rgba(29, 160, 88, 0.12)',
                borderWidth: 2.5,
                fill: true,
                tension: 0.35,
                pointRadius: currentData.length > 50 ? 0 : 3,
                pointHoverRadius: 6,
                pointBackgroundColor: '#1DA058'
            }
        ];

        if (previousData && previousData.length > 0) {
            datasets.push({
                label: 'Previous Period',
                data: previousData,
                borderColor: '#5A7E9B',
                backgroundColor: 'transparent',
                borderWidth: 1.8,
                borderDash: [5, 5],
                fill: false,
                tension: 0.35,
                pointRadius: 0,
                pointHoverRadius: 5,
                pointBackgroundColor: '#5A7E9B'
            });
        }

        this.instances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                        align: 'end',
                        labels: {
                            boxWidth: 12,
                            font: { family: '-apple-system, BlinkMacSystemFont, Segoe UI, Roboto', size: 12 },
                            color: '#03050B'
                        }
                    },
                    tooltip: {
                        backgroundColor: '#03050B',
                        titleColor: '#FFFFFF',
                        bodyColor: '#FFFFFF',
                        padding: 10,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ` ${context.dataset.label}: ${context.parsed.y.toFixed(2)} ${unit}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: {
                            color: '#5A7E9B',
                            font: { size: 11 },
                            maxRotation: 0,
                            autoSkip: true,
                            maxTicksLimit: 12
                        }
                    },
                    y: {
                        grid: { color: '#E2E8F0', drawBorder: false },
                        ticks: {
                            color: '#5A7E9B',
                            font: { size: 11 },
                            callback: function (value) {
                                return value + ' ' + unit;
                            }
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    },

    renderDoughnutChart: function (canvasId, labels, data, backgroundColors, centerText, centerLabel) {
        this.destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const defaultColors = ['#1DA058', '#39BF6D', '#5A7E9B', '#1E6A3F', '#A2B2BB', '#F59E0B'];
        const colors = backgroundColors && backgroundColors.length > 0 ? backgroundColors : defaultColors.slice(0, data.length);

        const centerTextPlugin = {
            id: 'centerTextPlugin',
            beforeDraw: function (chart) {
                if (!centerText) return;
                const width = chart.width;
                const height = chart.height;
                const ctx = chart.ctx;
                ctx.restore();

                const fontSize = Math.min(22, Math.round(height / 10));
                ctx.font = `700 ${fontSize}px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto`;
                ctx.textBaseline = 'middle';
                ctx.textAlign = 'center';
                ctx.fillStyle = '#03050B';

                const textX = Math.round(width / 2);
                const textY = Math.round(height / 2) - (centerLabel ? 8 : 0);
                ctx.fillText(centerText, textX, textY);

                if (centerLabel) {
                    ctx.font = `500 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto`;
                    ctx.fillStyle = '#5A7E9B';
                    ctx.fillText(centerLabel, textX, textY + 18);
                }

                ctx.save();
            }
        };

        this.instances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#FFFFFF',
                    hoverOffset: 6
                }]
            },
            plugins: [centerTextPlugin],
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '72%',
                plugins: {
                    legend: {
                        display: false // Displayed cleanly in custom markup below chart
                    },
                    tooltip: {
                        backgroundColor: '#03050B',
                        titleColor: '#FFFFFF',
                        bodyColor: '#FFFFFF',
                        padding: 10,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                const val = context.parsed;
                                return ` ${context.label}: ${val.toFixed(2)}`;
                            }
                        }
                    }
                }
            }
        });
    },

    renderBarChart: function (canvasId, labels, data, backgroundColors, label = 'Usage (kWh)') {
        this.destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const colors = backgroundColors && backgroundColors.length === data.length
            ? backgroundColors
            : '#1DA058';

        this.instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: colors,
                    borderRadius: 6,
                    borderSkipped: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#03050B',
                        cornerRadius: 8,
                        padding: 10
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: '#5A7E9B', font: { size: 11 } }
                    },
                    y: {
                        grid: { color: '#E2E8F0', drawBorder: false },
                        ticks: { color: '#5A7E9B', font: { size: 11 } },
                        beginAtZero: true
                    }
                }
            }
        });
    }
};
