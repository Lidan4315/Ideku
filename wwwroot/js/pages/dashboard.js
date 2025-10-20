// Dashboard Charts Module
const DashboardCharts = {
    charts: {},
    currentDivisionData: null,
    currentDivisionColor: null,
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
        this.loadStatusChart();
        this.loadDivisionChart();
        this.loadAllDepartmentsChart();
        this.loadStageByDivisionChart();
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

        const chartColors = [
            this.colors.primary,
            this.colors.warning,
            this.colors.success,
            this.colors.danger,
            this.colors.info,
            this.colors.purple
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

                // Dim non-hovered segments
                if (hoveredIndex !== null) {
                    meta.data.forEach((segment, index) => {
                        if (index !== hoveredIndex && !segment.hidden) {
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
                    backgroundColor: chartColors.slice(0, data.labels.length),
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: {
                    padding: {
                        top: 30,
                        right: 80,
                        bottom: 20,
                        left: 80
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
                        position: 'bottom',
                        align: 'center',
                        labels: {
                            padding: 15,
                            font: { size: 11 },
                            usePointStyle: false,
                            boxWidth: 15,
                            boxHeight: 15,
                            generateLabels: function(chart) {
                                const data = chart.data;
                                const meta = chart.getDatasetMeta(0);
                                return data.labels.map((label, i) => {
                                    const segment = meta.data[i];
                                    const isHidden = segment && segment.hidden;
                                    return {
                                        text: label,
                                        fillStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : chartColors[i],
                                        strokeStyle: isHidden ? 'rgba(128, 128, 128, 0.5)' : chartColors[i],
                                        lineWidth: 0,
                                        hidden: false,
                                        index: i,
                                        fontColor: isHidden ? 'rgba(128, 128, 128, 0.5)' : '#666'
                                    };
                                });
                            }
                        },
                        onClick: function(e, legendItem, legend) {
                            const index = legendItem.index;
                            const chart = legend.chart;
                            const meta = chart.getDatasetMeta(0);

                            // Toggle visibility (default Chart.js behavior)
                            meta.data[index].hidden = !meta.data[index].hidden;
                            chart.update();
                        },
                        onHover: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'pointer';
                            hoveredIndex = legendItem.index;
                            legend.chart.draw();
                        },
                        onLeave: function(event, legendItem, legend) {
                            event.native.target.style.cursor = 'default';
                            hoveredIndex = null;
                            legend.chart.draw();
                        }
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
                    barThickness: 40,
                    originalLabels: originalLabels
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
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
                maintainAspectRatio: true,
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
                maintainAspectRatio: true,
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

    init() {
        this.loadFiltersFromURL();
        this.initializeFilterControls();
    },

    initializeFilterControls() {
        const divisionSelect = document.getElementById('filterDivision');
        const stageSelect = document.getElementById('filterStage');
        const savingCostSelect = document.getElementById('filterSavingCost');
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

        if (divisionSelect) divisionSelect.value = '';
        if (stageSelect) stageSelect.value = '';
        if (savingCostSelect) savingCostSelect.value = '';

        this.currentDivision = null;
        this.currentStage = null;
        this.currentSavingCost = null;

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
