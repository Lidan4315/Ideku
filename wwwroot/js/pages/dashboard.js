// Dashboard Charts Module
const DashboardCharts = {
    charts: {},
    currentDivisionData: null,
    currentDivisionColor: null,
    statusChartHoveredIndex: null,
    colors: {
        primary: '#3b82f6',
        success: '#10b981',
        warning: '#f59e0b',
        danger: '#ef4444',
        info: '#06b6d4',
        purple: '#8b5cf6',
        pink: '#ec4899',
        indigo: '#6366f1'
    },

    init() {
        // Register datalabels plugin globally but disable by default
        if (typeof ChartDataLabels !== 'undefined') {
            Chart.register(ChartDataLabels);
            Chart.defaults.set('plugins.datalabels', {
                display: false
            });
        }

        this.loadStatusChart();
        this.loadDivisionChart();
        this.loadAllDepartmentsChart();
        this.loadStageByDivisionChart();
        this.loadWLChart();
        this.loadIdeasList();
        this.loadTeamRoleList();
        this.loadIdeaCostSavingList();
        this.loadApprovalHistoryList();
        this.initBackButton();
    },

    initBackButton() {
        const breadcrumbLink = document.getElementById('breadcrumbDivisions');
        if (breadcrumbLink) {
            breadcrumbLink.addEventListener('click', (e) => {
                e.preventDefault();
                this.loadDivisionChart();
            });
        }
    },

    loadStatusChart(queryString = '') {
        const url = `/Home/GetIdeasByStatusChart${queryString ? '?' + queryString : ''}`;
        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderStatusChart(response.data);
                }
            })
            .catch(err => console.error('Error loading status chart:', err));
    },

    renderStatusChart(data) {
        const ctx = document.getElementById('statusChart');
        if (!ctx) return;

        // Destroy existing chart if it exists
        if (this.charts.status) {
            this.charts.status.destroy();
        }

        // Colors array for stages (will cycle if more than 8 stages)
        const chartColors = [
            '#3b82f6',  // S0 - blue
            '#f59e0b',  // S1 - orange
            '#10b981',  // S2 - green
            '#ef4444',  // S3 - red
            '#06b6d4',  // S4 - cyan
            '#8b5cf6',  // S5 - purple
            '#ec4899',  // S6 - pink
            '#14b8a6'   // S7 - teal
        ];

        const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
        const self = this;
        let hoveredIndex = null;

        // Custom plugin to dim non-hovered segments and draw labels
        const hoverPlugin = {
            id: 'pieHoverHighlight',
            afterDraw(chart, args, options) {
                const ctx = chart.ctx;
                const meta = chart.getDatasetMeta(0);

                // Dim non-hovered segments (check both local hoveredIndex and global statusChartHoveredIndex)
                const currentHoveredIndex = hoveredIndex !== null ? hoveredIndex : self.statusChartHoveredIndex;
                if (currentHoveredIndex !== null) {
                    meta.data.forEach((segment, index) => {
                        if (index !== currentHoveredIndex && !segment.hidden) {
                            const {x, y, startAngle, endAngle, innerRadius, outerRadius} = segment;
                            ctx.save();
                            ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
                            ctx.beginPath();
                            ctx.arc(x, y, outerRadius, startAngle, endAngle);
                            ctx.arc(x, y, innerRadius, endAngle, startAngle, true);
                            ctx.closePath();
                            ctx.fill();
                            ctx.restore();
                        }
                    });
                }

                // Draw labels with lines
                ctx.save();
                ctx.textBaseline = 'middle';

                meta.data.forEach((segment, index) => {
                    if (segment.hidden) return;

                    const {x, y, startAngle, endAngle, outerRadius} = segment;
                    const midAngle = (startAngle + endAngle) / 2;

                    // Calculate positions
                    const lineStartRadius = outerRadius + 5;
                    const lineEndRadius = outerRadius + 25;
                    const textRadius = outerRadius + 30;

                    const x1 = x + Math.cos(midAngle) * lineStartRadius;
                    const y1 = y + Math.sin(midAngle) * lineStartRadius;
                    const x2 = x + Math.cos(midAngle) * lineEndRadius;
                    const y2 = y + Math.sin(midAngle) * lineEndRadius;
                    const x3 = x + Math.cos(midAngle) * textRadius;
                    const y3 = y + Math.sin(midAngle) * textRadius;

                    // Draw line (bold)
                    ctx.beginPath();
                    ctx.moveTo(x1, y1);
                    ctx.lineTo(x2, y2);
                    ctx.strokeStyle = '#666';
                    ctx.lineWidth = 2;
                    ctx.stroke();

                    // Prepare label text (2 lines)
                    const label = data.labels[index];
                    const value = data.datasets[0].data[index];
                    const percentage = ((value / total) * 100).toFixed(1);
                    const line1 = `${label}: ${value}`;
                    const line2 = `(${percentage}%)`;

                    // Text alignment based on position
                    const isRightSide = Math.cos(midAngle) >= 0;
                    ctx.textAlign = isRightSide ? 'left' : 'right';
                    ctx.fillStyle = '#333';

                    // Draw line 1 (bold)
                    ctx.font = 'bold 11px Arial';
                    ctx.fillText(line1, x3, y3 - 7);

                    // Draw line 2 (normal)
                    ctx.font = '10px Arial';
                    ctx.fillText(line2, x3, y3 + 7);
                });

                ctx.restore();
            }
        };

        this.charts.status = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: data.labels,
                datasets: [{
                    data: data.datasets[0].data,
                    backgroundColor: data.labels.map((label, index) => chartColors[index % chartColors.length]),
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: {
                    padding: {
                        top: 40,
                        right: 120,
                        bottom: 30,
                        left: 120
                    }
                },
                onHover: function(event, activeElements, chart) {
                    event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';

                    let newHoveredIndex = null;
                    if (activeElements.length > 0) {
                        newHoveredIndex = activeElements[0].index;
                    }

                    if (newHoveredIndex !== hoveredIndex) {
                        hoveredIndex = newHoveredIndex;
                        chart.draw();
                    }
                },
                plugins: {
                    pieHoverHighlight: {},
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        cornerRadius: 6,
                        titleFont: { size: 13 },
                        bodyFont: { size: 12 },
                        callbacks: {
                            label: function(context) {
                                const value = context.parsed;
                                const percentage = ((value / total) * 100).toFixed(1);
                                return context.label + ': ' + value + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [hoverPlugin]
        });

        // Handle mouse leave from canvas
        ctx.addEventListener('mouseleave', function() {
            hoveredIndex = null;
            if (self.charts.status) {
                self.charts.status.draw();
            }
        });

        // Generate custom HTML legend
        this.generateStatusChartLegend(data, chartColors);
    },

    generateStatusChartLegend(data, chartColors) {
        const legendContainer = document.getElementById('statusChartLegend');
        if (!legendContainer) return;

        const self = this;
        legendContainer.innerHTML = '';

        data.labels.forEach((label, index) => {
            const legendItem = document.createElement('div');
            legendItem.className = 'pie-chart-legend-item';
            legendItem.dataset.index = index;

            const colorBox = document.createElement('div');
            colorBox.className = 'pie-chart-legend-color';
            colorBox.style.backgroundColor = chartColors[index % chartColors.length];

            const textSpan = document.createElement('span');
            textSpan.className = 'pie-chart-legend-text';
            textSpan.textContent = label;

            legendItem.appendChild(colorBox);
            legendItem.appendChild(textSpan);
            legendContainer.appendChild(legendItem);

            // Add click event to toggle segment visibility
            legendItem.addEventListener('click', function() {
                const chart = self.charts.status;
                const meta = chart.getDatasetMeta(0);
                const segmentIndex = parseInt(this.dataset.index);

                meta.data[segmentIndex].hidden = !meta.data[segmentIndex].hidden;
                chart.update();

                // Toggle hidden class on legend item
                this.classList.toggle('hidden');
            });

            // Add hover event to highlight segment
            legendItem.addEventListener('mouseenter', function() {
                self.statusChartHoveredIndex = parseInt(this.dataset.index);
                self.charts.status.draw();
            });

            legendItem.addEventListener('mouseleave', function() {
                self.statusChartHoveredIndex = null;
                self.charts.status.draw();
            });
        });
    },

    loadDivisionChart(queryString = '', isDivisionLevel = true) {
        this.currentDivisionColor = null;
        const url = `/Home/GetIdeasByDivisionChart${queryString ? '?' + queryString : ''}`;
        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.currentDivisionData = response.data;
                    this.renderDivisionChart(response.data, isDivisionLevel);
                    document.getElementById('divisionChartTitle').textContent = 'Count of Initiative by Divisions';
                    document.getElementById('divisionBreadcrumb').style.display = 'none';
                }
            })
            .catch(err => console.error('Error loading division chart:', err));
    },

    loadDepartmentChart(divisionId, divisionName, divisionColor) {
        this.currentDivisionColor = divisionColor;
        fetch(`/Home/GetIdeasByDepartmentChart?divisionId=${divisionId}`)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderDivisionChart(response.data, false);
                    document.getElementById('divisionChartTitle').textContent = 'Count of Initiative by Divisions';
                    document.getElementById('breadcrumbCurrentDivision').textContent = response.data.divisionName;
                    document.getElementById('divisionBreadcrumb').style.display = 'block';
                }
            })
            .catch(err => console.error('Error loading department chart:', err));
    },

    renderDivisionChart(data, isDivisionLevel) {
        const ctx = document.getElementById('divisionChart');
        if (!ctx) return;

        if (this.charts.division) {
            this.charts.division.destroy();
        }

        const colors = [
            '#3b82f6', '#f59e0b', '#10b981', '#06b6d4', '#8b5cf6',
            '#ec4899', '#ef4444', '#14b8a6', '#f97316', '#a855f7',
            '#22c55e', '#eab308', '#64748b', '#0ea5e9', '#84cc16'
        ];

        const self = this;

        // For department level, use single color from the clicked division
        const chartColors = isDivisionLevel
            ? colors.slice(0, data.labels.length)
            : Array(data.labels.length).fill(this.currentDivisionColor);

        // Store original labels for tooltip
        const originalLabels = [...data.labels];

        // Truncate labels for display
        const truncatedLabels = data.labels.map(label => {
            if (label.length > 20) {
                return label.substring(0, 20) + '...';
            }
            return label;
        });

        this.charts.division = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: truncatedLabels,
                datasets: [{
                    label: 'Initiative #',
                    data: data.datasets[0].data,
                    backgroundColor: chartColors,
                    borderRadius: 6,
                    categoryPercentage: 0.8,
                    barPercentage: 0.7,
                    originalLabels: originalLabels
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                onHover: function(evt, activeElements, chart) {
                    if (isDivisionLevel) {
                        evt.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';
                    }
                },
                onClick: function(evt, activeElements, chart) {
                    if (isDivisionLevel) {
                        let index = -1;

                        if (activeElements.length > 0) {
                            index = activeElements[0].index;
                        } else {
                            const canvasPosition = Chart.helpers.getRelativePosition(evt, chart);
                            const dataX = chart.scales.x.getValueForPixel(canvasPosition.x);
                            if (dataX !== undefined && dataX >= 0 && dataX < data.labels.length) {
                                index = Math.floor(dataX);
                            }
                        }

                        if (index >= 0 && index < data.labels.length) {
                            const divisionId = data.divisionIds[index];
                            const divisionName = data.labels[index];
                            const divisionColor = colors[index % colors.length];
                            self.loadDepartmentChart(divisionId, divisionName, divisionColor);
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        cornerRadius: 6,
                        callbacks: {
                            title: function(context) {
                                const index = context[0].dataIndex;
                                const originalLabels = context[0].dataset.originalLabels;
                                return originalLabels ? originalLabels[index] : context[0].label;
                            },
                            label: function(context) {
                                return 'Initiative #: ' + context.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45,
                            font: {
                                size: 11
                            },
                            color: '#3b82f6'
                        }
                    }
                }
            }
        });

        if (isDivisionLevel) {
            ctx.parentElement.style.cursor = 'pointer';
            const parentDiv = ctx.closest('.card-body');
            if (parentDiv) {
                parentDiv.style.cursor = 'pointer';
                parentDiv.onclick = function(e) {
                    const rect = ctx.getBoundingClientRect();
                    const x = e.clientX - rect.left;
                    const chart = self.charts.division;
                    const dataX = chart.scales.x.getValueForPixel(x);

                    if (dataX !== undefined && dataX >= 0 && dataX < data.labels.length) {
                        const index = Math.round(dataX);
                        if (index >= 0 && index < data.labels.length) {
                            const divisionId = data.divisionIds[index];
                            const divisionName = data.labels[index];
                            const divisionColor = colors[index % colors.length];
                            self.loadDepartmentChart(divisionId, divisionName, divisionColor);
                        }
                    }
                };
            }
        } else {
            ctx.parentElement.style.cursor = 'default';
            const parentDiv = ctx.closest('.card-body');
            if (parentDiv) {
                parentDiv.style.cursor = 'default';
                parentDiv.onclick = null;
            }
        }
    },

    loadAllDepartmentsChart(queryString = '') {
        const url = `/Home/GetIdeasByAllDepartmentsChart${queryString ? '?' + queryString : ''}`;
        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderAllDepartmentsChart(response.data);
                }
            })
            .catch(err => console.error('Error loading all departments chart:', err));
    },

    loadStageByDivisionChart(queryString = '') {
        const url = `/Home/GetInitiativeByStageAndDivisionChart${queryString ? '?' + queryString : ''}`;
        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderStageByDivisionChart(response.data);
                }
            })
            .catch(err => console.error('Error loading stage by division chart:', err));
    },

    renderAllDepartmentsChart(data) {
        const ctx = document.getElementById('allDepartmentsChart');
        if (!ctx) return;

        if (this.charts.allDepartments) {
            this.charts.allDepartments.destroy();
        }

        const colors = [
            '#3b82f6', '#f59e0b', '#10b981', '#06b6d4', '#8b5cf6',
            '#ec4899', '#ef4444', '#14b8a6', '#f97316', '#a855f7',
            '#22c55e', '#eab308', '#64748b', '#0ea5e9', '#84cc16'
        ];

        // Store original labels for tooltip
        const originalLabels = [...data.labels];

        // Truncate labels for display
        const truncatedLabels = data.labels.map(label => {
            if (label.length > 20) {
                return label.substring(0, 20) + '...';
            }
            return label;
        });

        this.charts.allDepartments = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: truncatedLabels,
                datasets: [{
                    label: 'Initiative #',
                    data: data.datasets[0].data,
                    backgroundColor: colors.slice(0, data.labels.length),
                    borderRadius: 6,
                    categoryPercentage: 0.8,
                    barPercentage: 0.7,
                    originalLabels: originalLabels
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        cornerRadius: 6,
                        callbacks: {
                            title: function(context) {
                                const index = context[0].dataIndex;
                                const originalLabels = context[0].dataset.originalLabels;
                                return originalLabels ? originalLabels[index] : context[0].label;
                            },
                            label: function(context) {
                                return 'Initiative #: ' + context.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45,
                            font: {
                                size: 11
                            },
                            color: '#000000'
                        }
                    }
                }
            }
        });
    },

    renderStageByDivisionChart(data) {
        const ctx = document.getElementById('stageByDivisionChart');
        if (!ctx) return;

        if (this.charts.stageByDivision) {
            this.charts.stageByDivision.destroy();
        }

        const colors = [
            '#3b82f6', '#f59e0b', '#10b981', '#06b6d4', '#8b5cf6',
            '#ec4899', '#ef4444', '#14b8a6', '#f97316', '#a855f7',
            '#22c55e', '#eab308', '#64748b', '#0ea5e9', '#84cc16'
        ];

        // Prepare datasets with colors
        const datasets = data.datasets.map((dataset, index) => ({
            label: dataset.label,
            data: dataset.data,
            backgroundColor: colors[index % colors.length],
            borderRadius: 4
        }));

        const self = this;
        let hoveredDatasetIndex = null;

        // Custom plugin to dim non-hovered datasets
        const hoverPlugin = {
            id: 'hoverHighlight',
            afterDatasetsDraw(chart, args, options) {
                if (hoveredDatasetIndex !== null) {
                    const ctx = chart.ctx;
                    chart.data.datasets.forEach((dataset, index) => {
                        if (index !== hoveredDatasetIndex) {
                            const meta = chart.getDatasetMeta(index);
                            if (!meta.hidden) {
                                meta.data.forEach(bar => {
                                    const {x, y, width, height} = bar;
                                    ctx.save();
                                    ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
                                    ctx.fillRect(x - width / 2, y, width, height);
                                    ctx.restore();
                                });
                            }
                        }
                    });
                }
            }
        };

        this.charts.stageByDivision = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                onHover: function(event, activeElements, chart) {
                    event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';

                    let newHoveredIndex = null;
                    if (activeElements.length > 0) {
                        newHoveredIndex = activeElements[0].datasetIndex;
                    }

                    if (newHoveredIndex !== hoveredDatasetIndex) {
                        hoveredDatasetIndex = newHoveredIndex;
                        chart.draw();
                    }
                },
                plugins: {
                    hoverHighlight: {},
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            padding: 8,
                            font: {
                                size: 10
                            },
                            generateLabels: function(chart) {
                                const datasets = chart.data.datasets;
                                return datasets.map((dataset, i) => {
                                    const meta = chart.getDatasetMeta(i);
                                    const isHidden = meta.hidden;
                                    return {
                                        text: dataset.label,
                                        fillStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : dataset.backgroundColor,
                                        strokeStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : dataset.backgroundColor,
                                        lineWidth: 0,
                                        hidden: false,
                                        index: i,
                                        datasetIndex: i,
                                        fontColor: isHidden ? 'rgba(128, 128, 128, 0.5)' : '#666'
                                    };
                                });
                            }
                        },
                        onClick: function(e, legendItem, legend) {
                            const index = legendItem.datasetIndex;
                            const chart = legend.chart;
                            const meta = chart.getDatasetMeta(index);

                            // Toggle visibility (default Chart.js behavior)
                            meta.hidden = meta.hidden === null ? !chart.data.datasets[index].hidden : null;
                            chart.update();
                        },
                        onHover: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'pointer';
                            hoveredDatasetIndex = legendItem.datasetIndex;
                            legend.chart.draw();
                        },
                        onLeave: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'default';
                            hoveredDatasetIndex = null;
                            legend.chart.draw();
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        cornerRadius: 6,
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + context.parsed.y;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: true,
                        grid: {
                            display: false
                        },
                        ticks: {
                            font: {
                                size: 11
                            }
                        }
                    },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    }
                }
            },
            plugins: [hoverPlugin]
        });

        // Handle mouse leave from canvas
        ctx.addEventListener('mouseleave', function() {
            hoveredDatasetIndex = null;
            if (self.charts.stageByDivision) {
                self.charts.stageByDivision.draw();
            }
        });
    },

    getFilterParams() {
        const params = new URLSearchParams();

        // Helper function to format date as yyyy-MM-dd
        const formatDateForAPI = (date) => {
            if (!date) return null;
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`;
        };

        // Get date filters from DateFilter object
        if (DateFilter.currentStartDate) {
            const formattedStart = formatDateForAPI(DateFilter.currentStartDate);
            if (formattedStart) {
                params.set('startDate', formattedStart);
            }
        }
        if (DateFilter.currentEndDate) {
            const formattedEnd = formatDateForAPI(DateFilter.currentEndDate);
            if (formattedEnd) {
                params.set('endDate', formattedEnd);
            }
        }

        // Get other filters from DashboardFilter object
        if (DashboardFilter.currentDivision) {
            params.set('selectedDivision', DashboardFilter.currentDivision);
        }
        if (DashboardFilter.currentStage) {
            params.set('selectedStage', DashboardFilter.currentStage);
        }
        if (DashboardFilter.currentSavingCost) {
            params.set('savingCostRange', DashboardFilter.currentSavingCost);
        }
        if (DashboardFilter.currentInitiatorName) {
            params.set('initiatorName', DashboardFilter.currentInitiatorName);
        }
        if (DashboardFilter.currentInitiatorBadgeNumber) {
            params.set('initiatorBadgeNumber', DashboardFilter.currentInitiatorBadgeNumber);
        }
        if (DashboardFilter.currentIdeaId) {
            params.set('ideaId', DashboardFilter.currentIdeaId);
        }
        if (DashboardFilter.currentInitiatorDivision) {
            params.set('initiatorDivision', DashboardFilter.currentInitiatorDivision);
        }

        return params;
    },

    getVisiblePages(currentPage, totalPages, maxVisible = 5) {
        const pages = [];

        if (totalPages <= maxVisible) {
            // If total pages less than or equal to maxVisible, show all
            for (let i = 1; i <= totalPages; i++) {
                pages.push(i);
            }
        } else {
            // Show max pages around current page
            let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
            let endPage = Math.min(totalPages, startPage + maxVisible - 1);

            // Adjust start if we're near the end
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }

            for (let i = startPage; i <= endPage; i++) {
                pages.push(i);
            }
        }

        return pages;
    },

    // WL Chart Functions
    loadWLChart(queryString = '') {
        const url = `/Home/GetWLChart${queryString ? '?' + queryString : ''}`;
        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderWLChart(response.data);
                }
            })
            .catch(err => console.error('Error loading WL chart:', err));
    },

    renderWLChart(data) {
        const ctx = document.getElementById('wlChart');
        if (!ctx) return;

        if (this.charts.wl) {
            this.charts.wl.destroy();
        }

        // Prepare labels (WL names)
        const labels = data.map(wl => wl.userName || wl.employeeId);

        // Colors array for stages (will cycle if more than 8 stages)
        const stageColors = [
            '#3b82f6',  // S0 - blue
            '#f59e0b',  // S1 - orange
            '#10b981',  // S2 - green
            '#ef4444',  // S3 - red
            '#06b6d4',  // S4 - cyan
            '#8b5cf6',  // S5 - purple
            '#ec4899',  // S6 - pink
            '#14b8a6'   // S7 - teal (bukan gray)
        ];

        // Dynamically extract all unique stages from data
        const allStages = new Set();
        data.forEach(wl => {
            Object.keys(wl.ideasByStage).forEach(stageKey => {
                allStages.add(stageKey);
            });
        });

        // Sort stages numerically (S0, S1, S2, ... S8, S9, ...)
        const sortedStages = Array.from(allStages).sort((a, b) => {
            const numA = parseInt(a.replace('S', ''));
            const numB = parseInt(b.replace('S', ''));
            return numA - numB;
        });

        // Create datasets for each stage found in data
        const datasets = sortedStages.map((stageKey) => {
            const stageNum = parseInt(stageKey.replace('S', ''));
            return {
                label: stageKey,
                data: data.map(wl => wl.ideasByStage[stageKey] || 0),
                backgroundColor: stageColors[stageNum % stageColors.length],
                borderWidth: 0
            };
        });

        const self = this;
        let hoveredDatasetIndex = null;

        // Custom plugin to dim non-hovered datasets
        const hoverPlugin = {
            id: 'wlHoverHighlight',
            afterDatasetsDraw(chart, args, options) {
                if (hoveredDatasetIndex !== null) {
                    const ctx = chart.ctx;
                    chart.data.datasets.forEach((dataset, index) => {
                        if (index !== hoveredDatasetIndex) {
                            const meta = chart.getDatasetMeta(index);
                            if (!meta.hidden) {
                                meta.data.forEach(bar => {
                                    // For horizontal bar: x is end point, base is start point
                                    const {x, y, base, height} = bar;
                                    ctx.save();
                                    ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
                                    // Draw from base (left) to x (right), vertically centered
                                    ctx.fillRect(base, y - height / 2, x - base, height);
                                    ctx.restore();
                                });
                            }
                        }
                    });
                }
            }
        };

        this.charts.wl = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: datasets
            },
            plugins: [hoverPlugin],
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                onHover: function(event, activeElements, chart) {
                    event.native.target.style.cursor = activeElements.length > 0 ? 'pointer' : 'default';

                    let newHoveredIndex = null;
                    if (activeElements.length > 0) {
                        newHoveredIndex = activeElements[0].datasetIndex;
                    }

                    if (newHoveredIndex !== hoveredDatasetIndex) {
                        hoveredDatasetIndex = newHoveredIndex;
                        chart.draw();
                    }
                },
                scales: {
                    x: {
                        stacked: true,
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Number of Ideas'
                        }
                    },
                    y: {
                        stacked: true
                    }
                },
                plugins: {
                    wlHoverHighlight: {},
                    datalabels: {
                        display: true,
                        color: function(context) {
                            // Auto calculate text color based on background brightness
                            const bgColor = context.dataset.backgroundColor;
                            const rgb = parseInt(bgColor.substring(1), 16);
                            const r = (rgb >> 16) & 0xff;
                            const g = (rgb >> 8) & 0xff;
                            const b = (rgb >> 0) & 0xff;
                            const brightness = (r * 299 + g * 587 + b * 114) / 1000;
                            return brightness > 155 ? '#000' : '#fff';
                        },
                        font: {
                            weight: 'bold',
                            size: 11
                        },
                        formatter: function(value, context) {
                            return value > 0 ? value : '';
                        },
                        anchor: 'center',
                        align: 'center'
                    },
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            padding: 8,
                            font: { size: 11 },
                            generateLabels: function(chart) {
                                const datasets = chart.data.datasets;
                                return datasets.map((dataset, i) => {
                                    const meta = chart.getDatasetMeta(i);
                                    const isHidden = meta.hidden;
                                    return {
                                        text: dataset.label,
                                        fillStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : dataset.backgroundColor,
                                        strokeStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : dataset.backgroundColor,
                                        lineWidth: 0,
                                        hidden: false,
                                        index: i,
                                        datasetIndex: i,
                                        fontColor: isHidden ? 'rgba(128, 128, 128, 0.5)' : '#666'
                                    };
                                });
                            }
                        },
                        onClick: function(e, legendItem, legend) {
                            const index = legendItem.datasetIndex;
                            const chart = legend.chart;
                            const meta = chart.getDatasetMeta(index);
                            meta.hidden = meta.hidden === null ? !chart.data.datasets[index].hidden : null;
                            chart.update();
                        },
                        onHover: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'pointer';
                            hoveredDatasetIndex = legendItem.datasetIndex;
                            legend.chart.draw();
                        },
                        onLeave: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'default';
                            hoveredDatasetIndex = null;
                            legend.chart.draw();
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        padding: 12,
                        cornerRadius: 6,
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + context.parsed.x;
                            }
                        }
                    }
                }
            }
        });
    },

    // Ideas List Functions
    loadIdeasList(page = 1, pageSize = 10) {
        const params = this.getFilterParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const queryString = params.toString();
        const url = `/Home/GetIdeasList${queryString ? '?' + queryString : ''}`;

        fetch(url)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.renderIdeasList(data.data);
                    if (data.pagination) {
                        this.renderIdeasPagination(data.pagination);
                    }
                } else {
                    console.error('Failed to load ideas list:', data.message);
                }
            })
            .catch(error => {
                console.error('Error loading ideas list:', error);
            });
    },

    renderIdeasList(data) {
        const tbody = document.getElementById('ideasTableBody');
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="12" class="text-center text-muted">
                        <i class="bi bi-inbox me-2"></i>No ideas found.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = data.map(item => `
            <tr>
                <td>${item.ideaNumber}</td>
                <td><span class="badge ${this.getStatusBadgeClass(item.ideaStatus)}">${item.ideaStatus}</span></td>
                <td>${item.initiatorBN}</td>
                <td>${item.initiatorName}</td>
                <td>${item.initiatorDivision}</td>
                <td>${item.implementOnDivision}</td>
                <td>${item.implementOnDepartment}</td>
                <td>${item.ideaTitle}</td>
                <td><span class="badge bg-info">${item.currentStage}</span></td>
                <td>${this.formatDateTime(item.submissionDate)}</td>
                <td>${item.lastUpdatedDays}</td>
                <td>
                    <span class="badge ${this.getIdeaFlowBadgeClass(item.ideaFlowValidated)}">
                        ${this.getIdeaFlowDisplayText(item.ideaFlowValidated)}
                    </span>
                </td>
            </tr>
        `).join('');
    },

    renderIdeasPagination(paginationData) {
        const container = document.getElementById('ideasPaginationContainer');
        if (!container) return;

        if (!paginationData || paginationData.totalPages <= 1) {
            container.style.display = 'none';
            return;
        }

        container.style.display = 'flex';

        const infoSpan = container.querySelector('.pagination-info span');
        if (infoSpan) {
            infoSpan.textContent = `Showing ${paginationData.firstItemIndex}-${paginationData.lastItemIndex} of ${paginationData.totalCount} ideas`;
        }

        const paginationHTML = this.generateIdeasPaginationHTML(paginationData);
        const paginationUl = container.querySelector('.pagination');
        if (paginationUl) {
            paginationUl.innerHTML = paginationHTML;
        }

        this.attachIdeasPaginationHandlers(paginationData);

        const pageSizeSelect = document.getElementById('ideasPageSize');
        if (pageSizeSelect) {
            pageSizeSelect.value = paginationData.pageSize;
            pageSizeSelect.onchange = (e) => {
                this.loadIdeasList(1, parseInt(e.target.value));
            };
        }
    },

    generateIdeasPaginationHTML(paginationData) {
        let html = '';

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="1">
                    <i class="bi bi-chevron-double-left"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        const visiblePages = this.getVisiblePages(paginationData.currentPage, paginationData.totalPages);
        visiblePages.forEach(pageNum => {
            if (pageNum === '...') {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            } else {
                const isActive = pageNum === paginationData.currentPage;
                html += `
                    <li class="page-item ${isActive ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${pageNum}">${pageNum}</a>
                    </li>
                `;
            }
        });

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.totalPages}">
                    <i class="bi bi-chevron-double-right"></i>
                </a>
            </li>
        `;

        return html;
    },

    attachIdeasPaginationHandlers(paginationData) {
        const paginationLinks = document.querySelectorAll('#ideasPaginationContainer .page-link');
        const pageSize = document.getElementById('ideasPageSize')?.value || 10;

        paginationLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.currentTarget.getAttribute('data-page'));
                if (page && page >= 1 && page <= paginationData.totalPages) {
                    this.loadIdeasList(page, parseInt(pageSize));
                }
            });
        });
    },

    // Team Role List Functions
    loadTeamRoleList(page = 1, pageSize = 10) {
        const params = this.getFilterParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const queryString = params.toString();
        const url = `/Home/GetTeamRoleList${queryString ? '?' + queryString : ''}`;

        fetch(url)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.renderTeamRoleList(data.data);
                    if (data.pagination) {
                        this.renderTeamRolePagination(data.pagination);
                    }
                } else {
                    console.error('Failed to load team role list:', data.message);
                }
            })
            .catch(error => {
                console.error('Error loading team role list:', error);
            });
    },

    renderTeamRoleList(data) {
        const tbody = document.getElementById('teamRoleTableBody');
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="3" class="text-center text-muted">
                        <i class="bi bi-inbox me-2"></i>No team role data found.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = data.map(item => `
            <tr>
                <td>${item.employeeBN}</td>
                <td><span class="badge bg-primary">${item.teamRole}</span></td>
                <td>${item.ideaCode}</td>
            </tr>
        `).join('');
    },

    renderTeamRolePagination(paginationData) {
        const container = document.getElementById('teamRolePaginationContainer');
        if (!container) return;

        if (!paginationData || paginationData.totalPages <= 1) {
            container.style.display = 'none';
            return;
        }

        container.style.display = 'flex';

        const infoSpan = container.querySelector('.pagination-info span');
        if (infoSpan) {
            infoSpan.textContent = `Showing ${paginationData.firstItemIndex}-${paginationData.lastItemIndex} of ${paginationData.totalCount} roles`;
        }

        const paginationHTML = this.generateTeamRolePaginationHTML(paginationData);
        const paginationUl = container.querySelector('.pagination');
        if (paginationUl) {
            paginationUl.innerHTML = paginationHTML;
        }

        this.attachTeamRolePaginationHandlers(paginationData);

        const pageSizeSelect = document.getElementById('teamRolePageSize');
        if (pageSizeSelect) {
            pageSizeSelect.value = paginationData.pageSize;
            pageSizeSelect.onchange = (e) => {
                this.loadTeamRoleList(1, parseInt(e.target.value));
            };
        }
    },

    generateTeamRolePaginationHTML(paginationData) {
        let html = '';

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="1">
                    <i class="bi bi-chevron-double-left"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        const visiblePages = this.getVisiblePages(paginationData.currentPage, paginationData.totalPages, 3);
        visiblePages.forEach(pageNum => {
            if (pageNum === '...') {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            } else {
                const isActive = pageNum === paginationData.currentPage;
                html += `
                    <li class="page-item ${isActive ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${pageNum}">${pageNum}</a>
                    </li>
                `;
            }
        });

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.totalPages}">
                    <i class="bi bi-chevron-double-right"></i>
                </a>
            </li>
        `;

        return html;
    },

    attachTeamRolePaginationHandlers(paginationData) {
        const paginationLinks = document.querySelectorAll('#teamRolePaginationContainer .page-link');
        const pageSize = document.getElementById('teamRolePageSize')?.value || 10;

        paginationLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.currentTarget.getAttribute('data-page'));
                if (page && page >= 1 && page <= paginationData.totalPages) {
                    this.loadTeamRoleList(page, parseInt(pageSize));
                }
            });
        });
    },

    // Approval History List Functions
    loadApprovalHistoryList(page = 1, pageSize = 10) {
        const params = this.getFilterParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const queryString = params.toString();
        const url = `/Home/GetApprovalHistoryList${queryString ? '?' + queryString : ''}`;

        fetch(url)
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.renderApprovalHistoryList(data.data);
                    if (data.pagination) {
                        this.renderApprovalHistoryPagination(data.pagination);
                    }
                } else {
                    console.error('Failed to load approval history:', data.message);
                }
            })
            .catch(error => {
                console.error('Error loading approval history:', error);
            });
    },

    renderApprovalHistoryList(data) {
        const tbody = document.getElementById('approvalHistoryTableBody');
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="11" class="text-center text-muted">
                        <i class="bi bi-inbox me-2"></i>No approval history found.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = data.map(item => `
            <tr>
                <td>${item.ideaNumber}</td>
                <td>${item.approvalId}</td>
                <td><span class="badge ${this.getStatusBadgeClass(item.ideaStatus)}">${item.ideaStatus}</span></td>
                <td>${item.currentStage}</td>
                <td>${item.stageSequence}</td>
                <td>${this.formatDateTime(item.approvalDate)}</td>
                <td>${item.approver}</td>
                <td>${item.latestUpdateDate ? this.formatDateTime(item.latestUpdateDate) : 'N/A'}</td>
                <td>${item.lastUpdatedDays}</td>
                <td>${item.implementedDivision}</td>
                <td>${item.implementedDepartment}</td>
            </tr>
        `).join('');
    },

    renderApprovalHistoryPagination(paginationData) {
        const container = document.getElementById('approvalHistoryPaginationContainer');
        if (!container) return;

        if (!paginationData || paginationData.totalPages <= 1) {
            container.style.display = 'none';
            return;
        }

        container.style.display = 'flex';

        const infoSpan = container.querySelector('.pagination-info span');
        if (infoSpan) {
            infoSpan.textContent = `Showing ${paginationData.firstItemIndex}-${paginationData.lastItemIndex} of ${paginationData.totalCount} records`;
        }

        const paginationHTML = this.generateApprovalHistoryPaginationHTML(paginationData);
        const paginationUl = container.querySelector('.pagination');
        if (paginationUl) {
            paginationUl.innerHTML = paginationHTML;
        }

        this.attachApprovalHistoryPaginationHandlers(paginationData);

        const pageSizeSelect = document.getElementById('approvalHistoryPageSize');
        if (pageSizeSelect) {
            pageSizeSelect.value = paginationData.pageSize;
            pageSizeSelect.onchange = (e) => {
                this.loadApprovalHistoryList(1, parseInt(e.target.value));
            };
        }
    },

    generateApprovalHistoryPaginationHTML(paginationData) {
        let html = '';

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="1">
                    <i class="bi bi-chevron-double-left"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        const visiblePages = this.getVisiblePages(paginationData.currentPage, paginationData.totalPages);
        visiblePages.forEach(pageNum => {
            if (pageNum === '...') {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            } else {
                const isActive = pageNum === paginationData.currentPage;
                html += `
                    <li class="page-item ${isActive ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${pageNum}">${pageNum}</a>
                    </li>
                `;
            }
        });

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.totalPages}">
                    <i class="bi bi-chevron-double-right"></i>
                </a>
            </li>
        `;

        return html;
    },

    attachApprovalHistoryPaginationHandlers(paginationData) {
        const paginationLinks = document.querySelectorAll('#approvalHistoryPaginationContainer .page-link');
        const pageSize = document.getElementById('approvalHistoryPageSize')?.value || 10;

        paginationLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.currentTarget.getAttribute('data-page'));
                if (page && page >= 1 && page <= paginationData.totalPages) {
                    this.loadApprovalHistoryList(page, parseInt(pageSize));
                }
            });
        });
    },

    formatDateTime(dateString) {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleString('en-US', {
            year: 'numeric',
            month: 'numeric',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
    },

    getStatusBadgeClass(status) {
        const statusLower = status.toLowerCase();

        if (statusLower.includes('approved')) {
            return 'bg-success';
        } else if (statusLower.includes('rejected')) {
            return 'bg-danger';
        } else if (statusLower.includes('waiting')) {
            return 'bg-warning text-dark';
        } else if (statusLower.includes('unvalidated')) {
            return 'bg-secondary';
        } else if (statusLower.includes('on going')) {
            return 'bg-info';
        } else if (statusLower.includes('completed')) {
            return 'bg-primary';
        } else {
            return 'bg-secondary';
        }
    },

    // Idea Cost Saving List Functions
    loadIdeaCostSavingList(page = 1, pageSize = 10) {
        const params = this.getFilterParams();
        params.set('page', page);
        params.set('pageSize', pageSize);

        const queryString = params.toString();
        const url = `/Home/GetIdeaCostSavingList${queryString ? '?' + queryString : ''}`;

        fetch(url)
            .then(res => res.json())
            .then(response => {
                if (response.success) {
                    this.renderIdeaCostSavingList(response.data);
                    if (response.pagination) {
                        this.renderIdeaCostSavingPagination(response.pagination);
                    }
                } else {
                    console.error('Failed to load idea cost saving list:', response.message);
                }
            })
            .catch(err => console.error('Error loading idea cost saving list:', err));
    },

    renderIdeaCostSavingList(data) {
        const tbody = document.getElementById('ideaCostSavingTableBody');
        if (!tbody) return;

        if (!data || data.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-muted">
                        <i class="bi bi-inbox me-2"></i>No idea cost saving data found.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = data.map(item => `
            <tr>
                <td>${item.ideaId}</td>
                <td>$${item.savingCostValidated.toLocaleString()}</td>
                <td>${item.ideaCategory}</td>
                <td>
                    <span class="badge bg-info">
                        ${item.currentStage}
                    </span>
                </td>
                <td>
                    <span class="badge ${this.getIdeaFlowBadgeClass(item.ideaFlowValidated)}">
                        ${this.getIdeaFlowDisplayText(item.ideaFlowValidated)}
                    </span>
                </td>
            </tr>
        `).join('');
    },

    getIdeaFlowBadgeClass(flowStatus) {
        switch (flowStatus) {
            case 'more_than_20':
                return 'bg-success';
            case 'less_than_20':
                return 'bg-warning';
            case 'not_validated':
                return 'bg-secondary';
            default:
                return 'bg-secondary';
        }
    },

    getIdeaFlowDisplayText(flowStatus) {
        switch (flowStatus) {
            case 'more_than_20':
                return 'More than $20k';
            case 'less_than_20':
                return 'Less than $20k';
            case 'not_validated':
                return 'Not Validated';
            default:
                return flowStatus;
        }
    },

    renderIdeaCostSavingPagination(paginationData) {
        const container = document.getElementById('ideaCostSavingPaginationContainer');
        if (!container) return;

        if (!paginationData || paginationData.totalPages <= 1) {
            container.style.display = 'none';
            return;
        }

        container.style.display = 'flex';

        const infoSpan = container.querySelector('.pagination-info span');
        if (infoSpan) {
            infoSpan.textContent = `Showing ${paginationData.firstItemIndex}-${paginationData.lastItemIndex} of ${paginationData.totalCount} ideas`;
        }

        const paginationHTML = this.generateIdeaCostSavingPaginationHTML(paginationData);
        const paginationUl = container.querySelector('.pagination');
        if (paginationUl) {
            paginationUl.innerHTML = paginationHTML;
        }

        this.attachIdeaCostSavingPaginationHandlers(paginationData);

        const pageSizeSelect = document.getElementById('ideaCostSavingPageSize');
        if (pageSizeSelect) {
            pageSizeSelect.value = paginationData.pageSize;
            pageSizeSelect.onchange = (e) => {
                this.loadIdeaCostSavingList(1, parseInt(e.target.value));
            };
        }
    },

    generateIdeaCostSavingPaginationHTML(paginationData) {
        let html = '';

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="1">
                    <i class="bi bi-chevron-double-left"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasPrevious ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        const visiblePages = this.getVisiblePages(paginationData.currentPage, paginationData.totalPages);
        visiblePages.forEach(pageNum => {
            if (pageNum === '...') {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            } else {
                const isActive = pageNum === paginationData.currentPage;
                html += `
                    <li class="page-item ${isActive ? 'active' : ''}">
                        <a class="page-link" href="#" data-page="${pageNum}">${pageNum}</a>
                    </li>
                `;
            }
        });

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;

        html += `
            <li class="page-item ${!paginationData.hasNext ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${paginationData.totalPages}">
                    <i class="bi bi-chevron-double-right"></i>
                </a>
            </li>
        `;

        return html;
    },

    attachIdeaCostSavingPaginationHandlers(paginationData) {
        const paginationLinks = document.querySelectorAll('#ideaCostSavingPaginationContainer .page-link');
        const pageSize = document.getElementById('ideaCostSavingPageSize')?.value || 10;

        paginationLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.currentTarget.getAttribute('data-page'));
                if (page && page >= 1 && page <= paginationData.totalPages) {
                    this.loadIdeaCostSavingList(page, parseInt(pageSize));
                }
            });
        });
    },

    destroy() {
        Object.values(this.charts).forEach(chart => chart.destroy());
        this.charts = {};
    }
};

// Date Filter Module
const DateFilter = {
    dateRangePicker: null,
    currentStartDate: null,
    currentEndDate: null,

    init() {
        this.initializeDatePickers();
        this.initializeShortcutButtons();
        this.initializeCustomDaysInputs();
        this.initializeYearFilter();
        this.initializeApplyButton();
        this.initializeClearButton();

        // Apply date filters from URL if they exist (loaded by DashboardFilter.loadFiltersFromURL)
        if (this.currentStartDate && this.currentEndDate) {
            this.updateDisplayFields();
            // Use setTimeout to ensure calendar is fully rendered
            setTimeout(() => {
                this.highlightRange();
            }, 100);
        }
    },

    initializeDatePickers() {
        this.isSelectingRange = false;
        this.hoverDate = null;

        // Initialize Flatpickr without range mode - we'll handle range selection manually
        this.dateRangePicker = flatpickr('#dateRange', {
            inline: true,
            dateFormat: 'Y-m-d',
            maxDate: 'today',
            mode: 'multiple',
            onDayCreate: (dObj, dStr, fp, dayElem) => {
                // Add hover handler for range preview
                dayElem.addEventListener('mouseenter', (e) => {
                    if (this.isSelectingRange && this.currentStartDate) {
                        this.hoverDate = dayElem.dateObj;
                        this.highlightRange();
                    }
                });

                dayElem.addEventListener('mouseleave', (e) => {
                    if (this.isSelectingRange) {
                        this.hoverDate = null;
                        this.highlightRange();
                    }
                });

                // Add custom click handler to each day
                dayElem.addEventListener('click', (e) => {
                    e.stopPropagation();
                    const clickedDate = dayElem.dateObj;

                    if (!this.isSelectingRange) {
                        // First click or reset after completed selection - set both to same date
                        this.currentStartDate = clickedDate;
                        this.currentEndDate = clickedDate;
                        this.isSelectingRange = true;
                        this.hoverDate = null;
                        this.dateRangePicker.setDate([clickedDate]);
                        this.highlightRange();
                        this.updateDisplayFields();
                    } else {
                        // Second click - confirm end date for range
                        if (clickedDate >= this.currentStartDate) {
                            this.currentEndDate = clickedDate;
                        } else {
                            // If clicked date is before start, swap them
                            this.currentEndDate = this.currentStartDate;
                            this.currentStartDate = clickedDate;
                        }
                        this.isSelectingRange = false;
                        this.hoverDate = null;
                        this.highlightRange();
                        this.updateDisplayFields();

                        // Auto-apply filter when range is selected
                        this.applyFilter();
                    }
                });
            },
            onMonthChange: () => {
                // Re-highlight range when month changes
                setTimeout(() => this.highlightRange(), 50);
            },
            onYearChange: () => {
                // Re-highlight range when year changes
                setTimeout(() => this.highlightRange(), 50);
            },
            onReady: () => {
                // Apply highlighting when calendar is ready
                setTimeout(() => this.highlightRange(), 50);
            }
        });
    },

    highlightRange() {
        if (!this.dateRangePicker) return;

        const calendarContainer = this.dateRangePicker.calendarContainer;
        const dayElems = calendarContainer.querySelectorAll('.flatpickr-day');

        dayElems.forEach(dayElem => {
            // Remove all range classes first
            dayElem.classList.remove('inRange', 'startRange', 'endRange', 'inRangePreview', 'endRangePreview');

            // Skip if no date object or no start date set
            if (!dayElem.dateObj || !this.currentStartDate) {
                return;
            }

            const dayTime = dayElem.dateObj.getTime();
            const startTime = this.currentStartDate.getTime();

            // Use hover date for preview if selecting range, otherwise use actual end date
            const endTime = this.isSelectingRange && this.hoverDate
                ? this.hoverDate.getTime()
                : (this.currentEndDate ? this.currentEndDate.getTime() : startTime);

            const actualStart = Math.min(startTime, endTime);
            const actualEnd = Math.max(startTime, endTime);

            // Check if we're in preview mode (selecting range AND hovering over a date)
            const isPreviewMode = this.isSelectingRange && this.hoverDate;

            if (isPreviewMode) {
                // Preview mode - show preview styling
                if (dayTime === actualStart && dayTime === actualEnd) {
                    dayElem.classList.add('startRange', 'endRange');
                } else if (dayTime === actualStart) {
                    dayElem.classList.add('startRange');
                } else if (dayTime === actualEnd) {
                    dayElem.classList.add('endRangePreview');
                } else if (dayTime > actualStart && dayTime < actualEnd) {
                    dayElem.classList.add('inRangePreview');
                }
            } else {
                // Confirmed mode OR selecting but not hovering - use solid fill style
                if (dayTime === actualStart && dayTime === actualEnd) {
                    dayElem.classList.add('startRange', 'endRange');
                } else if (dayTime === actualStart) {
                    dayElem.classList.add('startRange');
                } else if (dayTime === actualEnd) {
                    dayElem.classList.add('endRange');
                } else if (dayTime > actualStart && dayTime < actualEnd) {
                    dayElem.classList.add('inRange');
                }
            }
        });
    },

    updateDisplayFields() {
        const startDisplay = document.getElementById('startDateDisplay');
        const endDisplay = document.getElementById('endDateDisplay');

        if (startDisplay && this.currentStartDate) {
            startDisplay.value = this.formatDate(this.currentStartDate);
        } else if (startDisplay) {
            startDisplay.value = '';
        }

        if (endDisplay && this.currentEndDate) {
            endDisplay.value = this.formatDate(this.currentEndDate);
        } else if (endDisplay) {
            endDisplay.value = '';
        }
    },

    initializeShortcutButtons() {
        document.querySelectorAll('.date-filter-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const filter = e.target.dataset.filter;
                this.applyQuickFilter(filter);
            });
        });
    },

    initializeCustomDaysInputs() {
        // Days up to today
        const daysUpInput = document.getElementById('daysUpToToday');
        if (daysUpInput) {
            daysUpInput.addEventListener('change', () => {
                const days = parseInt(daysUpInput.value) || 1;
                const endDate = new Date();
                const startDate = new Date();
                startDate.setDate(startDate.getDate() - days);

                this.setDateRange(startDate, endDate);
            });
        }

        // Days from today
        const daysFromInput = document.getElementById('daysFromToday');
        if (daysFromInput) {
            daysFromInput.addEventListener('change', () => {
                const days = parseInt(daysFromInput.value) || 1;
                const startDate = new Date();
                const endDate = new Date();
                endDate.setDate(endDate.getDate() + days);

                this.setDateRange(startDate, endDate);
            });
        }
    },

    initializeYearFilter() {
        const yearSelect = document.getElementById('filterYear');
        if (!yearSelect) return;

        // Populate year dropdown with last 6 years (current year + 5 previous years)
        const currentYear = new Date().getFullYear();
        const yearsToShow = 6;

        // Add individual years from current year down
        for (let i = 0; i < yearsToShow; i++) {
            const year = currentYear - i;
            const option = document.createElement('option');
            option.value = year;
            option.textContent = year;
            yearSelect.appendChild(option);
        }

        // Add change event listener
        yearSelect.addEventListener('change', (e) => {
            const value = e.target.value;
            if (!value) return;

            // Specific year selected - filter full year (Jan 1 - Dec 31)
            const year = parseInt(value);
            const startDate = new Date(year, 0, 1); // Jan 1
            const endDate = new Date(year, 11, 31); // Dec 31
            this.setDateRange(startDate, endDate);
            this.applyFilter();

            // Reset dropdown to default after applying
            yearSelect.value = '';
        });
    },

    initializeApplyButton() {
        const applyBtn = document.getElementById('applyDateFilter');
        if (applyBtn) {
            applyBtn.addEventListener('click', () => {
                this.applyFilter();
            });
        }
    },

    initializeClearButton() {
        const clearBtn = document.getElementById('clearDateFilter');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                this.clearFilter();
            });
        }

        // Export button handler
        const exportBtn = document.getElementById('exportDashboard');
        if (exportBtn) {
            console.log('Export button found, attaching event listener');
            exportBtn.addEventListener('click', (e) => {
                e.preventDefault();
                console.log('Export button clicked');
                this.exportDashboard();
            });
        } else {
            console.error('Export button not found!');
        }
    },

    exportDashboard() {
        console.log('exportDashboard called');

        // Get the hidden form
        const form = document.getElementById('exportForm');
        const startDateInput = document.getElementById('exportStartDate');
        const endDateInput = document.getElementById('exportEndDate');
        const divisionInput = document.getElementById('exportDivision');
        const stageInput = document.getElementById('exportStage');
        const savingCostInput = document.getElementById('exportSavingCost');
        const initiatorDivisionInput = document.getElementById('exportInitiatorDivision');
        const initiatorNameInput = document.getElementById('exportInitiatorName');
        const initiatorBadgeNumberInput = document.getElementById('exportInitiatorBadgeNumber');
        const ideaIdInput = document.getElementById('exportIdeaId');

        // Set date values if filters are active
        if (this.currentStartDate && this.currentEndDate) {
            // Format tanggal lokal (tidak menggunakan toISOString untuk menghindari timezone issue)
            const formatLocalDate = (date) => {
                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                return `${year}-${month}-${day}`;
            };

            startDateInput.value = formatLocalDate(this.currentStartDate);
            endDateInput.value = formatLocalDate(this.currentEndDate);
            console.log('Date filters:', {
                start: startDateInput.value,
                end: endDateInput.value
            });
        } else {
            startDateInput.value = '';
            endDateInput.value = '';
        }

        // Set dashboard filter values
        if (DashboardFilter.currentDivision) {
            divisionInput.value = DashboardFilter.currentDivision;
        } else {
            divisionInput.value = '';
        }

        if (DashboardFilter.currentStage) {
            stageInput.value = DashboardFilter.currentStage;
        } else {
            stageInput.value = '';
        }

        if (DashboardFilter.currentSavingCost) {
            savingCostInput.value = DashboardFilter.currentSavingCost;
        } else {
            savingCostInput.value = '';
        }

        // Set 4 advanced filter values
        if (DashboardFilter.currentInitiatorDivision) {
            initiatorDivisionInput.value = DashboardFilter.currentInitiatorDivision;
        } else {
            initiatorDivisionInput.value = '';
        }

        if (DashboardFilter.currentInitiatorName) {
            initiatorNameInput.value = DashboardFilter.currentInitiatorName;
        } else {
            initiatorNameInput.value = '';
        }

        if (DashboardFilter.currentInitiatorBadgeNumber) {
            initiatorBadgeNumberInput.value = DashboardFilter.currentInitiatorBadgeNumber;
        } else {
            initiatorBadgeNumberInput.value = '';
        }

        if (DashboardFilter.currentIdeaId) {
            ideaIdInput.value = DashboardFilter.currentIdeaId;
        } else {
            ideaIdInput.value = '';
        }

        console.log('Submitting export form with all filters');

        // Submit the form - this will trigger a file download
        form.submit();
    },

    normalizeDate(date) {
        // Normalize date to midnight (00:00:00) to match Flatpickr's date objects
        const normalized = new Date(date);
        normalized.setHours(0, 0, 0, 0);
        return normalized;
    },

    applyQuickFilter(filterType) {
        const today = new Date();
        let startDate, endDate;

        switch (filterType) {
            case 'today':
                startDate = this.normalizeDate(today);
                endDate = this.normalizeDate(today);
                break;
            case 'yesterday':
                const yesterday = new Date(today);
                yesterday.setDate(today.getDate() - 1);
                startDate = this.normalizeDate(yesterday);
                endDate = this.normalizeDate(yesterday);
                break;
            case 'thisWeek':
                const weekStart = new Date(today);
                weekStart.setDate(today.getDate() - today.getDay());
                startDate = this.normalizeDate(weekStart);
                endDate = this.normalizeDate(today);
                break;
            case 'lastWeek':
                const lastWeekStart = new Date(today);
                lastWeekStart.setDate(today.getDate() - today.getDay() - 7);
                const lastWeekEnd = new Date(lastWeekStart);
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = this.normalizeDate(lastWeekStart);
                endDate = this.normalizeDate(lastWeekEnd);
                break;
            case 'thisMonth':
                startDate = this.normalizeDate(new Date(today.getFullYear(), today.getMonth(), 1));
                endDate = this.normalizeDate(today);
                break;
            case 'lastMonth':
                startDate = this.normalizeDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
                endDate = this.normalizeDate(new Date(today.getFullYear(), today.getMonth(), 0));
                break;
            default:
                return;
        }

        this.setDateRange(startDate, endDate);
        this.applyFilter();
    },

    setDateRange(startDate, endDate) {
        this.currentStartDate = startDate;
        this.currentEndDate = endDate;
        this.isSelectingRange = false;
        this.hoverDate = null;

        if (this.dateRangePicker) {
            // Clear dates first
            this.dateRangePicker.clear();

            // Jump to the correct month
            this.dateRangePicker.jumpToDate(startDate);

            // Force Flatpickr to redraw the calendar
            this.dateRangePicker.redraw();

            // Apply highlighting with delays to ensure DOM is ready
            setTimeout(() => {
                this.highlightRange();
            }, 50);

            setTimeout(() => {
                this.highlightRange();
            }, 150);

            setTimeout(() => {
                this.highlightRange();
            }, 300);
        }

        this.updateDisplayFields();
    },

    applyFilter() {
        if (!this.currentStartDate || !this.currentEndDate) {
            alert('Please select both start and end dates');
            return;
        }

        // Update URL with date filter
        const queryString = DashboardFilter.buildQueryString(this.currentStartDate, this.currentEndDate);
        DashboardFilter.updateURL(queryString);

        // Reload all charts with date filter (pass Date objects, not strings)
        this.reloadDashboard(this.currentStartDate, this.currentEndDate);
    },

    clearFilter() {
        this.currentStartDate = null;
        this.currentEndDate = null;
        this.isSelectingRange = false;
        this.hoverDate = null;

        if (this.dateRangePicker) {
            this.dateRangePicker.clear();
        }

        this.highlightRange();
        this.updateDisplayFields();

        // Update URL (remove date filters, keep dashboard filters)
        const queryString = DashboardFilter.buildQueryString(null, null);
        DashboardFilter.updateURL(queryString);

        // Reload dashboard without filter
        this.reloadDashboard(null, null);
    },

    formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    },

    async reloadDashboard(startDate, endDate) {
        try {
            // Use DashboardFilter to build query string with ALL filters (date + dashboard filters)
            const queryString = DashboardFilter.buildQueryString(startDate, endDate);

            // Reload statistics
            await this.reloadStatistics(queryString);

            // Reload all charts
            DashboardCharts.loadStatusChart(queryString);
            DashboardCharts.loadDivisionChart(queryString);
            DashboardCharts.loadAllDepartmentsChart(queryString);
            DashboardCharts.loadStageByDivisionChart(queryString);
            DashboardCharts.loadWLChart(queryString);

            // Reload all tables
            DashboardCharts.loadIdeasList();
            DashboardCharts.loadTeamRoleList();
            DashboardCharts.loadIdeaCostSavingList();
            DashboardCharts.loadApprovalHistoryList();
        } catch (error) {
            console.error('Error reloading dashboard:', error);
            alert('Error applying filter. Please try again.');
        }
    },

    async reloadStatistics(queryString) {
        try {
            const url = `/Home/GetDashboardStatistics${queryString ? '?' + queryString : ''}`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                // Update Total Ideas
                const totalIdeasEl = document.querySelector('.stat-card h2');
                if (totalIdeasEl && totalIdeasEl.textContent.trim() !== '') {
                    totalIdeasEl.textContent = result.totalIdeas;
                }

                // Update Saving Cost
                const savingCostEls = document.querySelectorAll('.stat-card h2');
                if (savingCostEls.length > 1) {
                    savingCostEls[1].textContent = `$ ${result.totalSavingCost.toLocaleString()}`;
                }

                const validatedCostEl = document.querySelector('.stat-card small');
                if (validatedCostEl) {
                    validatedCostEl.textContent = `Validated: $ ${result.validatedSavingCost.toLocaleString()}`;
                }
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }
};

// Dashboard Filter Module
const DashboardFilter = {
    // Properties
    currentDivision: null,
    currentStage: null,
    currentSavingCost: null,
    currentInitiatorDivision: null,
    currentInitiatorName: null,
    currentInitiatorBadgeNumber: null,
    currentIdeaId: null,

    init() {
        this.loadFiltersFromURL();
        this.initializeFilterControls();
    },

    initializeFilterControls() {
        const divisionSelect = document.getElementById('filterDivision');
        const stageSelect = document.getElementById('filterStage');
        const savingCostSelect = document.getElementById('filterSavingCost');
        const initiatorDivisionSelect = document.getElementById('filterInitiatorDivision');
        const initiatorNameInput = document.getElementById('filterInitiatorName');
        const initiatorBadgeNumberInput = document.getElementById('filterInitiatorBadgeNumber');
        const ideaIdInput = document.getElementById('filterIdeaId');
        const clearBtn = document.getElementById('clearAllFilters');

        if (divisionSelect) {
            divisionSelect.addEventListener('change', () => this.applyFilters());
        }

        if (stageSelect) {
            stageSelect.addEventListener('change', () => this.applyFilters());
        }

        if (savingCostSelect) {
            savingCostSelect.addEventListener('change', () => this.applyFilters());
        }

        if (initiatorDivisionSelect) {
            initiatorDivisionSelect.addEventListener('change', () => this.applyFilters());
        }

        // Auto-apply for text inputs with debounce (500ms)
        if (initiatorNameInput) {
            let timeout;
            initiatorNameInput.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(() => this.applyFilters(), 500);
            });
        }

        if (initiatorBadgeNumberInput) {
            let timeout;
            initiatorBadgeNumberInput.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(() => this.applyFilters(), 500);
            });
        }

        if (ideaIdInput) {
            let timeout;
            ideaIdInput.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(() => this.applyFilters(), 500);
            });
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', () => this.clearFilters());
        }
    },

    loadFiltersFromURL() {
        // Read URL parameters
        const urlParams = new URLSearchParams(window.location.search);

        const divisionSelect = document.getElementById('filterDivision');
        const stageSelect = document.getElementById('filterStage');
        const savingCostSelect = document.getElementById('filterSavingCost');
        const initiatorDivisionSelect = document.getElementById('filterInitiatorDivision');
        const initiatorNameInput = document.getElementById('filterInitiatorName');
        const initiatorBadgeNumberInput = document.getElementById('filterInitiatorBadgeNumber');
        const ideaIdInput = document.getElementById('filterIdeaId');

        // Load Division filter from URL
        const urlDivision = urlParams.get('selectedDivision');
        if (urlDivision && divisionSelect) {
            divisionSelect.value = urlDivision;
            this.currentDivision = urlDivision;
        } else {
            this.currentDivision = divisionSelect?.value || null;
        }

        // Load Stage filter from URL
        const urlStage = urlParams.get('selectedStage');
        if (urlStage && stageSelect) {
            stageSelect.value = urlStage;
            this.currentStage = urlStage;
        } else {
            this.currentStage = stageSelect?.value || null;
        }

        // Load Saving Cost filter from URL
        const urlSavingCost = urlParams.get('savingCostRange');
        if (urlSavingCost && savingCostSelect) {
            savingCostSelect.value = urlSavingCost;
            this.currentSavingCost = urlSavingCost;
        } else {
            this.currentSavingCost = savingCostSelect?.value || null;
        }

        // Load Initiator Division filter from URL
        const urlInitiatorDivision = urlParams.get('initiatorDivision');
        if (urlInitiatorDivision && initiatorDivisionSelect) {
            initiatorDivisionSelect.value = urlInitiatorDivision;
            this.currentInitiatorDivision = urlInitiatorDivision;
        } else {
            this.currentInitiatorDivision = initiatorDivisionSelect?.value || null;
        }

        // Load Initiator Name filter from URL
        const urlInitiatorName = urlParams.get('initiatorName');
        if (urlInitiatorName && initiatorNameInput) {
            initiatorNameInput.value = urlInitiatorName;
            this.currentInitiatorName = urlInitiatorName;
        } else {
            this.currentInitiatorName = initiatorNameInput?.value || null;
        }

        // Load Initiator Badge Number filter from URL
        const urlInitiatorBadgeNumber = urlParams.get('initiatorBadgeNumber');
        if (urlInitiatorBadgeNumber && initiatorBadgeNumberInput) {
            initiatorBadgeNumberInput.value = urlInitiatorBadgeNumber;
            this.currentInitiatorBadgeNumber = urlInitiatorBadgeNumber;
        } else {
            this.currentInitiatorBadgeNumber = initiatorBadgeNumberInput?.value || null;
        }

        // Load Idea Id filter from URL
        const urlIdeaId = urlParams.get('ideaId');
        if (urlIdeaId && ideaIdInput) {
            ideaIdInput.value = urlIdeaId;
            this.currentIdeaId = urlIdeaId;
        } else {
            this.currentIdeaId = ideaIdInput?.value || null;
        }

        // Load Date filters from URL
        const urlStartDate = urlParams.get('startDate');
        const urlEndDate = urlParams.get('endDate');
        if (urlStartDate && urlEndDate) {
            const startDate = new Date(urlStartDate + 'T00:00:00');
            const endDate = new Date(urlEndDate + 'T00:00:00');
            DateFilter.currentStartDate = startDate;
            DateFilter.currentEndDate = endDate;
        }
    },

    applyFilters() {
        // Get current filter values
        const divisionSelect = document.getElementById('filterDivision');
        const stageSelect = document.getElementById('filterStage');
        const savingCostSelect = document.getElementById('filterSavingCost');

        this.currentDivision = divisionSelect?.value || null;
        this.currentStage = stageSelect?.value || null;
        this.currentSavingCost = savingCostSelect?.value || null;

        // Get 4 advanced filter values
        const initiatorDivisionSelect = document.getElementById('filterInitiatorDivision');
        const initiatorNameInput = document.getElementById('filterInitiatorName');
        const initiatorBadgeNumberInput = document.getElementById('filterInitiatorBadgeNumber');
        const ideaIdInput = document.getElementById('filterIdeaId');

        this.currentInitiatorDivision = initiatorDivisionSelect?.value || null;
        this.currentInitiatorName = initiatorNameInput?.value || null;
        this.currentInitiatorBadgeNumber = initiatorBadgeNumberInput?.value || null;
        this.currentIdeaId = ideaIdInput?.value || null;

        // Get current date filter from DateFilter module
        const startDate = DateFilter.currentStartDate;
        const endDate = DateFilter.currentEndDate;

        // Build query string with ALL filters
        const queryString = this.buildQueryString(startDate, endDate);

        // Update URL without page reload
        this.updateURL(queryString);

        // Reload dashboard with filters
        this.reloadDashboardWithFilters(queryString);
    },

    updateURL(queryString) {
        // Update browser URL without page reload using History API
        const newURL = queryString
            ? `${window.location.pathname}?${queryString}`
            : window.location.pathname;

        window.history.pushState({}, '', newURL);
    },

    buildQueryString(startDate, endDate) {
        const params = new URLSearchParams();

        // Date filters
        if (startDate) {
            params.append('startDate', DateFilter.formatDate(startDate));
        }
        if (endDate) {
            params.append('endDate', DateFilter.formatDate(endDate));
        }

        // Dashboard filters
        if (this.currentDivision) {
            params.append('selectedDivision', this.currentDivision);
        }
        if (this.currentStage) {
            params.append('selectedStage', this.currentStage);
        }
        if (this.currentSavingCost) {
            params.append('savingCostRange', this.currentSavingCost);
        }

        // 4 Advanced filters
        if (this.currentInitiatorDivision) {
            params.append('initiatorDivision', this.currentInitiatorDivision);
        }
        if (this.currentInitiatorName) {
            params.append('initiatorName', this.currentInitiatorName);
        }
        if (this.currentInitiatorBadgeNumber) {
            params.append('initiatorBadgeNumber', this.currentInitiatorBadgeNumber);
        }
        if (this.currentIdeaId) {
            params.append('ideaId', this.currentIdeaId);
        }

        return params.toString();
    },

    async reloadDashboardWithFilters(queryString) {
        try {
            // Reload statistics
            await this.reloadStatistics(queryString);

            // Reload all charts
            DashboardCharts.loadStatusChart(queryString);
            DashboardCharts.loadDivisionChart(queryString);
            DashboardCharts.loadAllDepartmentsChart(queryString);
            DashboardCharts.loadStageByDivisionChart(queryString);
            DashboardCharts.loadWLChart(queryString);

            // Reload all tables
            DashboardCharts.loadIdeasList();
            DashboardCharts.loadTeamRoleList();
            DashboardCharts.loadIdeaCostSavingList();
            DashboardCharts.loadApprovalHistoryList();
        } catch (error) {
            console.error('Error applying dashboard filters:', error);
        }
    },

    async reloadStatistics(queryString) {
        try {
            const url = `/Home/GetDashboardStatistics${queryString ? '?' + queryString : ''}`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                // Update Total Ideas
                const totalIdeasEl = document.querySelector('.stat-card h2');
                if (totalIdeasEl && totalIdeasEl.textContent.trim() !== '') {
                    totalIdeasEl.textContent = result.totalIdeas;
                }

                // Update Saving Cost
                const savingCostEls = document.querySelectorAll('.stat-card h2');
                if (savingCostEls.length > 1) {
                    savingCostEls[1].textContent = `$ ${result.totalSavingCost.toLocaleString()}`;
                }

                const validatedCostEl = document.querySelector('.stat-card small');
                if (validatedCostEl) {
                    validatedCostEl.textContent = `Validated: $ ${result.validatedSavingCost.toLocaleString()}`;
                }
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    },

    clearFilters() {
        // Reset filter dropdowns
        const divisionSelect = document.getElementById('filterDivision');
        const stageSelect = document.getElementById('filterStage');
        const savingCostSelect = document.getElementById('filterSavingCost');
        const initiatorDivisionSelect = document.getElementById('filterInitiatorDivision');
        const initiatorNameInput = document.getElementById('filterInitiatorName');
        const initiatorBadgeNumberInput = document.getElementById('filterInitiatorBadgeNumber');
        const ideaIdInput = document.getElementById('filterIdeaId');

        if (divisionSelect) divisionSelect.value = '';
        if (stageSelect) stageSelect.value = '';
        if (savingCostSelect) savingCostSelect.value = '';
        if (initiatorDivisionSelect) initiatorDivisionSelect.value = '';
        if (initiatorNameInput) initiatorNameInput.value = '';
        if (initiatorBadgeNumberInput) initiatorBadgeNumberInput.value = '';
        if (ideaIdInput) ideaIdInput.value = '';

        this.currentDivision = null;
        this.currentStage = null;
        this.currentSavingCost = null;
        this.currentInitiatorDivision = null;
        this.currentInitiatorName = null;
        this.currentInitiatorBadgeNumber = null;
        this.currentIdeaId = null;

        // Clear URL parameters (keep only date filters if they exist)
        const startDate = DateFilter.currentStartDate;
        const endDate = DateFilter.currentEndDate;
        const queryString = this.buildQueryString(startDate, endDate);
        this.updateURL(queryString);

        // Apply filters (which will clear them)
        this.applyFilters();
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    // IMPORTANT: Load filters from URL first before initializing charts
    DashboardFilter.init();  // This loads URL parameters including date filters
    DateFilter.init();       // This renders calendar with loaded dates
    DashboardCharts.init();  // This renders charts with applied filters
});
