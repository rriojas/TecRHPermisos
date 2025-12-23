// Función para decodificar caracteres HTML escapados
function decodeHtml(str) {
    const txt = document.createElement('textarea');
    txt.innerHTML = str;
    return txt.value;
}

// Parsear datos del DTO con manejo de errores
let permisosPorTipo, permisosPorArea, tendencia, heatmapData, kpiData, conGoce, sinGoce;
try {
    const permisosPorTipoRaw = document.getElementById('permisosPorTipo')?.value || '[]';
    const permisosPorAreaRaw = document.getElementById('permisosPorArea')?.value || '[]';
    const tendenciaRaw = document.getElementById('tendencia')?.value || '[]';
    const heatmapDataRaw = document.getElementById('heatmapData')?.value || '[]';
    const kpiDataRaw = document.getElementById('kpiData')?.value || '{}';
    const conGoceRaw = document.getElementById('conGoce')?.value || '0';
    const sinGoceRaw = document.getElementById('sinGoce')?.value || '0';

    // Decodificar valores HTML antes de parsear
    permisosPorTipo = JSON.parse(decodeHtml(permisosPorTipoRaw));
    permisosPorArea = JSON.parse(decodeHtml(permisosPorAreaRaw));
    tendencia = JSON.parse(decodeHtml(tendenciaRaw));
    heatmapData = JSON.parse(decodeHtml(heatmapDataRaw));
    kpiData = JSON.parse(decodeHtml(kpiDataRaw));
    conGoce = parseInt(conGoceRaw);
    sinGoce = parseInt(sinGoceRaw);

    console.log('Datos cargados:', { permisosPorTipo, permisosPorArea, tendencia, kpiData, conGoce, sinGoce });
} catch (e) {
    console.error('Error parseando datos:', e);
    permisosPorTipo = [];
    permisosPorArea = [];
    tendencia = [];
    heatmapData = [];
    kpiData = { total: 0, aprobados: 0, pendientes: 0, diasPromedio: 0 };
    conGoce = 0;
    sinGoce = 0;
}

// Crear gráfico de dona con D3.js
function createDonutChart() {
    if (!permisosPorTipo.length) {
        document.getElementById('donut-chart').innerHTML = '<p class="text-gray-500 text-center py-8">No hay datos disponibles</p>';
        document.getElementById('donut-legend').innerHTML = '';
        return;
    }
    
    const width = 280;
    const height = 280;
    const radius = Math.min(width, height) / 2 - 20;
    const innerRadius = radius * 0.6;

    // Limpiar contenedor
    d3.select("#donut-chart").html("");

    const svg = d3.select("#donut-chart")
        .append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${width / 2}, ${height / 2})`);

    const pie = d3.pie().value(d => d.Value).sort(null);
    const arc = d3.arc().innerRadius(innerRadius).outerRadius(radius).cornerRadius(8);
    const hoverArc = d3.arc().innerRadius(innerRadius).outerRadius(radius + 10).cornerRadius(8);

    const arcs = svg.selectAll(".arc")
        .data(pie(permisosPorTipo))
        .enter().append("g")
        .attr("class", "arc");

    arcs.append("path")
        .attr("d", arc)
        .attr("fill", d => d.data.Color)
        .attr("stroke", "white")
        .attr("stroke-width", 3)
        .style("filter", "drop-shadow(0 4px 8px rgba(0, 0, 0, 0.1))")
        .style("cursor", "pointer")
        .on("mouseover", function (event, d) {
            d3.select(this).transition().duration(200).attr("d", hoverArc)
                .style("filter", "drop-shadow(0 6px 12px rgba(0, 0, 0, 0.2))");
        })
        .on("mouseout", function (event, d) {
            d3.select(this).transition().duration(200).attr("d", arc)
                .style("filter", "drop-shadow(0 4px 8px rgba(0, 0, 0, 0.1))");
        });

    const total = permisosPorTipo.reduce((sum, d) => sum + d.Value, 0);
    
    svg.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", "-0.5em")
        .style("font-size", "32px")
        .style("font-weight", "bold")
        .style("fill", "#1f2937")
        .text(total);

    svg.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", "1em")
        .style("font-size", "14px")
        .style("fill", "#6b7280")
        .text("Total");

    // Crear leyenda
    const legend = d3.select("#donut-legend")
        .selectAll(".legend-item")
        .data(permisosPorTipo)
        .enter().append("div")
        .attr("class", "flex items-center justify-between p-3 rounded-xl")
        .style("background-color", d => `rgba(${parseInt(d.Color.slice(1, 3), 16)}, ${parseInt(d.Color.slice(3, 5), 16)}, ${parseInt(d.Color.slice(5, 7), 16)}, 0.1)`);

    legend.append("div")
        .attr("class", "flex items-center gap-3")
        .html(d => `<div class="w-4 h-4 rounded-full shadow-sm" style="background-color: ${d.Color}"></div><span class="text-sm font-medium text-gray-700">${d.Name}</span>`);

    legend.append("div")
        .attr("class", "text-sm font-bold")
        .style("color", d => d.Color)
        .text(d => `${d.Value} (${(d.Value / total * 100).toFixed(1)}%)`);
}

function animateDonut() {
    if (!permisosPorTipo.length) return;
    d3.select("#donut-chart svg").remove();
    createDonutChart();
}

// Crear gráfico de área con ApexCharts
let areaChart;
function createAreaChart() {
    if (!tendencia.length) {
        document.getElementById('area-chart').innerHTML = '<p class="text-gray-500 text-center py-8">No hay datos disponibles</p>';
        return;
    }
    
    const options = {
        chart: {
            type: 'area',
            height: 350,
            animations: { enabled: true, easing: 'easeinout', speed: 800 },
            toolbar: { show: false },
            background: 'transparent'
        },
        series: [
            { name: 'Con Goce', data: tendencia.map(t => t.Aprobados || 0), color: '#10b981' },
            { name: 'Sin Goce', data: tendencia.map(t => t.Pendientes || 0), color: '#f59e0b' },
            { name: 'Total', data: tendencia.map(t => t.Total || 0), color: '#3b82f6' }
        ],
        xaxis: {
            categories: tendencia.map(t => t.Month || ''),
            axisBorder: { show: false },
            axisTicks: { show: false },
            labels: { style: { colors: '#6b7280', fontSize: '12px', fontWeight: 500 } }
        },
        yaxis: { 
            labels: { style: { colors: '#6b7280', fontSize: '12px', fontWeight: 500 } },
            title: { text: 'Cantidad de Permisos', style: { color: '#6b7280', fontSize: '12px' } }
        },
        fill: {
            type: 'gradient',
            gradient: { shade: 'light', type: 'vertical', shadeIntensity: 0.5, opacityFrom: 0.8, opacityTo: 0.1 }
        },
        stroke: { width: 3, curve: 'smooth' },
        grid: { borderColor: 'rgba(156, 163, 175, 0.2)', strokeDashArray: 3 },
        tooltip: { 
            theme: 'dark', 
            style: { fontSize: '12px' }, 
            x: { show: true },
            y: {
                formatter: function(value) {
                    return value + ' permisos';
                }
            }
        },
        legend: { 
            show: true,
            position: 'top',
            horizontalAlign: 'right',
            fontSize: '12px',
            markers: { width: 12, height: 12, radius: 12 }
        },
        markers: { size: 6, strokeWidth: 3, strokeColors: '#ffffff', hover: { size: 8 } }
    };
    areaChart = new ApexCharts(document.querySelector("#area-chart"), options);
    areaChart.render();
}

function changeChartType(type) {
    if (areaChart && tendencia.length) {
        areaChart.updateOptions({ chart: { type: type } });
    }
}

// Crear barras horizontales con D3.js
function createHorizontalBars() {
    if (!permisosPorArea.length) {
        document.getElementById('horizontal-bars').innerHTML = '<p class="text-gray-500 text-center py-8">No hay datos disponibles</p>';
        return;
    }
    
    const margin = { top: 20, right: 40, bottom: 40, left: 200 };
    const containerWidth = document.getElementById('horizontal-bars').offsetWidth;
    const width = containerWidth - margin.left - margin.right;
    const height = Math.max(300, permisosPorArea.length * 50);

    // Limpiar contenedor
    d3.select("#horizontal-bars").html("");

    const svg = d3.select("#horizontal-bars")
        .append("svg")
        .attr("width", containerWidth)
        .attr("height", height + margin.top + margin.bottom)
        .append("g")
        .attr("transform", `translate(${margin.left}, ${margin.top})`);

    const x = d3.scaleLinear()
        .domain([0, d3.max(permisosPorArea, d => d.Value) * 1.1])
        .range([0, width]);

    const y = d3.scaleBand()
        .domain(permisosPorArea.map(d => d.Name))
        .range([0, height])
        .padding(0.2);

    // Barras
    svg.selectAll(".bar")
        .data(permisosPorArea)
        .enter().append("rect")
        .attr("class", "bar")
        .attr("x", 0)
        .attr("y", d => y(d.Name))
        .attr("width", 0)
        .attr("height", y.bandwidth())
        .attr("fill", d => d.Color)
        .attr("rx", 8)
        .style("filter", "drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1))")
        .transition()
        .duration(1000)
        .delay((d, i) => i * 100)
        .attr("width", d => x(d.Value));

    // Etiquetas de área
    svg.selectAll(".label")
        .data(permisosPorArea)
        .enter().append("text")
        .attr("class", "label")
        .attr("x", -10)
        .attr("y", d => y(d.Name) + y.bandwidth() / 2)
        .attr("text-anchor", "end")
        .attr("alignment-baseline", "middle")
        .style("font-size", "11px")
        .style("font-weight", "600")
        .style("fill", "#374151")
        .text(d => d.Name.length > 30 ? d.Name.substring(0, 27) + '...' : d.Name);

    // Valores
    svg.selectAll(".value")
        .data(permisosPorArea)
        .enter().append("text")
        .attr("class", "value")
        .attr("x", d => x(d.Value) + 5)
        .attr("y", d => y(d.Name) + y.bandwidth() / 2)
        .attr("alignment-baseline", "middle")
        .style("font-size", "12px")
        .style("font-weight", "bold")
        .style("fill", "#374151")
        .text(d => d.Value);
}

function animateBars() {
    d3.select("#horizontal-bars svg").remove();
    createHorizontalBars();
}

// Crear gráfico radial con ApexCharts - Con Goce vs Pendientes por Revisar
function createRadialChart() {
    const total = kpiData.total || 0;
    if (total === 0) {
        document.getElementById('radial-chart').innerHTML = '<p class="text-gray-500 text-center py-8">No hay datos disponibles</p>';
        return;
    }
    
    const porcentajeConGoce = total > 0 ? parseFloat(((conGoce / total) * 100).toFixed(1)) : 0;
    const porcentajePendientes = total > 0 ? parseFloat(((sinGoce / total) * 100).toFixed(1)) : 0;
    
    const options = {
        chart: {
            type: 'radialBar',
            height: 300,
            animations: { enabled: true, easing: 'easeinout', speed: 800 }
        },
        plotOptions: {
            radialBar: {
                offsetY: 0,
                startAngle: 0,
                endAngle: 360,
                hollow: { 
                    margin: 15, 
                    size: '50%', 
                    background: 'transparent' 
                },
                dataLabels: {
                    name: { 
                        show: true, 
                        fontSize: '13px',
                        fontWeight: 600,
                        offsetY: 20,
                        color: '#374151'
                    },
                    value: { 
                        show: true, 
                        fontSize: '22px', 
                        fontWeight: 'bold', 
                        offsetY: -15,
                        color: '#1f2937',
                        formatter: function(val) { 
                            return parseInt(val) + '%'; 
                        } 
                    },
                    total: {
                        show: true,
                        label: 'Total Permisos',
                        fontSize: '13px',
                        fontWeight: 600,
                        color: '#6b7280',
                        formatter: function(w) {
                            return total;
                        }
                    }
                },
                track: { 
                    background: '#f3f4f6', 
                    strokeWidth: '100%',
                    margin: 5
                }
            }
        },
        colors: ['#10b981', '#f59e0b'],
        series: [porcentajeConGoce, porcentajePendientes],
        labels: ['Con Goce de Pago', 'Pendientes por Revisar'],
        legend: { 
            show: true, 
            position: 'bottom',
            fontSize: '13px',
            fontWeight: 600,
            offsetY: 5,
            markers: { width: 14, height: 14, radius: 12 },
            itemMargin: { horizontal: 10, vertical: 5 },
            formatter: function(seriesName, opts) {
                const value = opts.w.globals.series[opts.seriesIndex];
                const count = opts.seriesIndex === 0 ? conGoce : sinGoce;
                return seriesName + ': ' + value.toFixed(1) + '% (' + count + ')';
            }
        },
        stroke: {
            lineCap: 'round'
        },
        tooltip: {
            enabled: true,
            y: {
                formatter: function(value, { seriesIndex }) {
                    const count = seriesIndex === 0 ? conGoce : sinGoce;
                    return count + ' permisos (' + value.toFixed(1) + '%)';
                }
            }
        }
    };
    const radialChart = new ApexCharts(document.querySelector("#radial-chart"), options);
    radialChart.render();
}

// Crear mapa de calor con D3.js
function createHeatmap() {
    if (!heatmapData.length) {
        document.getElementById('heatmap').innerHTML = '<p class="text-gray-500 text-center py-8">No hay datos disponibles</p>';
        return;
    }
    
    const margin = { top: 40, right: 20, bottom: 30, left: 80 };
    const cellSize = 70;
    const weeks = [...new Set(heatmapData.map(d => `Semana ${d.Week}`))].sort();
    const days = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];
    
    const width = days.length * cellSize + margin.left + margin.right;
    const height = weeks.length * cellSize + margin.top + margin.bottom;

    // Limpiar contenedor
    d3.select("#heatmap").html("");

    const svg = d3.select("#heatmap")
        .append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${margin.left}, ${margin.top})`);

    // Mapear Day (DayOfWeek enum) a índice de día
    const dayMap = {
        'Monday': 0, 'Tuesday': 1, 'Wednesday': 2, 'Thursday': 3,
        'Friday': 4, 'Saturday': 5, 'Sunday': 6
    };

    const colorScale = d3.scaleSequential()
        .interpolator(d3.interpolateBlues)
        .domain([0, d3.max(heatmapData, d => d.Count) || 1]);

    // Celdas
    svg.selectAll(".cell")
        .data(heatmapData)
        .enter().append("rect")
        .attr("class", "cell")
        .attr("x", d => dayMap[d.Day] * cellSize)
        .attr("y", d => weeks.indexOf(`Semana ${d.Week}`) * cellSize)
        .attr("width", cellSize - 4)
        .attr("height", cellSize - 4)
        .attr("rx", 8)
        .style("fill", d => d.Count === 0 ? '#f3f4f6' : colorScale(d.Count))
        .style("stroke", "white")
        .style("stroke-width", 2)
        .style("cursor", "pointer")
        .style("opacity", 0)
        .on("mouseover", function (event, d) {
            d3.select(this).style("stroke", "#3b82f6").style("stroke-width", 3);
        })
        .on("mouseout", function () {
            d3.select(this).style("stroke", "white").style("stroke-width", 2);
        })
        .transition()
        .duration(800)
        .delay((d, i) => i * 30)
        .style("opacity", 1);

    // Etiquetas de días
    svg.selectAll(".day-label")
        .data(days)
        .enter().append("text")
        .attr("class", "day-label")
        .attr("x", (d, i) => i * cellSize + cellSize / 2)
        .attr("y", -15)
        .attr("text-anchor", "middle")
        .style("font-size", "11px")
        .style("font-weight", "600")
        .style("fill", "#6b7280")
        .text(d => d.substring(0, 3));

    // Etiquetas de semanas
    svg.selectAll(".week-label")
        .data(weeks)
        .enter().append("text")
        .attr("class", "week-label")
        .attr("x", -10)
        .attr("y", (d, i) => i * cellSize + cellSize / 2)
        .attr("text-anchor", "end")
        .attr("alignment-baseline", "middle")
        .style("font-size", "11px")
        .style("font-weight", "600")
        .style("fill", "#6b7280")
        .text(d => d);

    // Valores en celdas
    svg.selectAll(".cell-value")
        .data(heatmapData.filter(d => d.Count > 0))
        .enter().append("text")
        .attr("class", "cell-value")
        .attr("x", d => dayMap[d.Day] * cellSize + cellSize / 2)
        .attr("y", d => weeks.indexOf(`Semana ${d.Week}`) * cellSize + cellSize / 2)
        .attr("text-anchor", "middle")
        .attr("alignment-baseline", "middle")
        .style("font-size", "16px")
        .style("font-weight", "bold")
        .style("fill", "white")
        .style("opacity", 0)
        .text(d => d.Count)
        .transition()
        .duration(800)
        .delay((d, i) => i * 30 + 400)
        .style("opacity", 1);
}

function updateHeatmap() {
    d3.select("#heatmap svg").remove();
    createHeatmap();
}

// Inicializar todos los gráficos al cargar la página
document.addEventListener('DOMContentLoaded', function () {
    console.log('Inicializando visualizaciones de Cortes...');
    
    createDonutChart();
    createAreaChart();
    createHorizontalBars();
    createRadialChart();
    createHeatmap();

    // Animación de entrada para las cards
    const cards = document.querySelectorAll('.card-hover');
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        setTimeout(() => {
            card.style.transition = 'all 0.6s ease-out';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });

    console.log('Visualizaciones cargadas exitosamente');
});