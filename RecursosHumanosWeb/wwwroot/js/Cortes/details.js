// Función para decodificar caracteres HTML escapados
function decodeHtml(str) {
    const txt = document.createElement('textarea');
    txt.innerHTML = str;
    return txt.value;
}

// Parsear datos del ViewBag con manejo de errores
let permisosPorTipo, permisosPorArea, tendencia, heatmapData, kpiData, conGoce, sinGoce;
try {
    const permisosPorTipoRaw = document.getElementById('permisosPorTipo')?.value || '[]';
    const permisosPorAreaRaw = document.getElementById('permisosPorArea')?.value || '[]';
    const tendenciaRaw = document.getElementById('tendencia')?.value || '[]';
    const heatmapDataRaw = document.getElementById('heatmapData')?.value || '[]';
    const kpiDataRaw = document.getElementById('kpiData')?.value || '{}';
    const conGoceRaw = document.getElementById('conGoce')?.value || '0';
    const sinGoceRaw = document.getElementById('sinGoce')?.value || '0';

    // Log valores crudos para depuración
    console.log('permisosPorTipoRaw:', permisosPorTipoRaw);
    console.log('permisosPorAreaRaw:', permisosPorAreaRaw);
    console.log('tendenciaRaw:', tendenciaRaw);
    console.log('heatmapDataRaw:', heatmapDataRaw);
    console.log('kpiDataRaw:', kpiDataRaw);
    console.log('conGoceRaw:', conGoceRaw);
    console.log('sinGoceRaw:', sinGoceRaw);

    // Decodificar valores HTML antes de parsear
    const permisosPorTipoDecoded = decodeHtml(permisosPorTipoRaw);
    const permisosPorAreaDecoded = decodeHtml(permisosPorAreaRaw);
    const tendenciaDecoded = decodeHtml(tendenciaRaw);
    const heatmapDataDecoded = decodeHtml(heatmapDataRaw);
    const kpiDataDecoded = decodeHtml(kpiDataRaw);

    // Log valores decodificados para depuración
    console.log('permisosPorTipoDecoded:', permisosPorTipoDecoded);
    console.log('permisosPorAreaDecoded:', permisosPorAreaDecoded);
    console.log('tendenciaDecoded:', tendenciaDecoded);
    console.log('heatmapDataDecoded:', heatmapDataDecoded);
    console.log('kpiDataDecoded:', kpiDataDecoded);

    // Parsear los valores decodificados
    permisosPorTipo = JSON.parse(permisosPorTipoDecoded);
    permisosPorArea = JSON.parse(permisosPorAreaDecoded);
    tendencia = JSON.parse(tendenciaDecoded);
    heatmapData = JSON.parse(heatmapDataDecoded);
    kpiData = JSON.parse(kpiDataDecoded);
    conGoce = parseInt(conGoceRaw);
    sinGoce = parseInt(sinGoceRaw);

    // Log datos parseados
    console.log('PermisosPorTipo:', permisosPorTipo);
    console.log('PermisosPorArea:', permisosPorArea);
    console.log('Tendencia:', tendencia);
    console.log('HeatmapData:', heatmapData);
    console.log('KPIData:', kpiData);
    console.log('ConGoce:', conGoce);
    console.log('SinGoce:', sinGoce);
} catch (e) {
    console.error('Error parseando datos de ViewBag:', e);
    permisosPorTipo = [];
    permisosPorArea = [];
    tendencia = [];
    heatmapData = [];
    kpiData = { TotalPermisos: 0, Aprobados: 0, Pendientes: 0, DiasPromedio: 0 };
    conGoce = 0;
    sinGoce = 0;
}

// Actualizar KPIs
document.getElementById('kpi-total').textContent = kpiData.TotalPermisos || 0;
document.getElementById('kpi-total-change').textContent = kpiData.TotalPermisos > 0 ? `+${(kpiData.TotalPermisos / (kpiData.TotalPermisos + 1) * 100).toFixed(1)}% vs mes anterior` : 'Sin datos';
document.getElementById('kpi-aprobados').textContent = kpiData.Aprobados || 0;
document.getElementById('kpi-aprobados-pct').textContent = kpiData.TotalPermisos > 0 ? `${(kpiData.Aprobados / kpiData.TotalPermisos * 100).toFixed(1)}% del total` : '0%';
document.getElementById('kpi-pendientes').textContent = kpiData.Pendientes || 0;
document.getElementById('kpi-pendientes-pct').textContent = kpiData.TotalPermisos > 0 ? `${(kpiData.Pendientes / kpiData.TotalPermisos * 100).toFixed(1)}% del total` : '0%';
document.getElementById('kpi-dias').textContent = kpiData.DiasPromedio || 0;

function createDonutChart() {
    console.log('Creando Donut Chart con datos:', permisosPorTipo);
    if (!permisosPorTipo.length) {
        console.warn('No hay datos para Donut Chart');
        document.getElementById('donut-chart').innerHTML = '<p class="text-gray-500 text-center">No hay datos disponibles</p>';
        return;
    }
    const width = 280;
    const height = 280;
    const radius = Math.min(width, height) / 2 - 20;
    const innerRadius = radius * 0.6;

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

    svg.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", "-0.5em")
        .style("font-size", "32px")
        .style("font-weight", "bold")
        .style("fill", "#1f2937")
        .text(permisosPorTipo.reduce((sum, d) => sum + d.Value, 0) || 0);

    svg.append("text")
        .attr("text-anchor", "middle")
        .attr("dy", "1em")
        .style("font-size", "14px")
        .style("fill", "#6b7280")
        .text("Total");

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
        .text(d => `${d.Value} (${(d.Value / (permisosPorTipo.reduce((sum, d) => sum + d.Value, 0) || 1) * 100).toFixed(1)}%)`);
}

function animateDonut() {
    if (!permisosPorTipo.length) return;
    const arcs = d3.selectAll(".arc path");
    arcs.transition()
        .duration(1000)
        .attrTween("d", function (d) {
            const i = d3.interpolate({ startAngle: 0, endAngle: 0 }, d);
            return t => arc(i(t));
        });
}

let areaChart;
function createAreaChart() {
    console.log('Creando Area Chart con datos:', tendencia);
    if (!tendencia.length) {
        console.warn('No hay datos para Area Chart');
        document.getElementById('area-chart').innerHTML = '<p class="text-gray-500 text-center">No hay datos disponibles</p>';
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
            { name: 'Aprobados', data: tendencia.map(t => t.Aprobados || 0), color: '#10b981' },
            { name: 'Pendientes', data: tendencia.map(t => t.Pendientes || 0), color: '#f59e0b' },
            { name: 'Total', data: tendencia.map(t => t.Total || 0), color: '#3b82f6' }
        ],
        xaxis: {
            categories: tendencia.map(t => t.Month || ''),
            axisBorder: { show: false },
            axisTicks: { show: false },
            labels: { style: { colors: '#6b7280', fontSize: '12px', fontWeight: 500 } }
        },
        yaxis: { labels: { style: { colors: '#6b7280', fontSize: '12px', fontWeight: 500 } } },
        fill: {
            type: 'gradient',
            gradient: { shade: 'light', type: 'vertical', shadeIntensity: 0.5, opacityFrom: 0.8, opacityTo: 0.1 }
        },
        stroke: { width: 3, curve: 'smooth' },
        grid: { borderColor: 'rgba(156, 163, 175, 0.2)', strokeDashArray: 3 },
        tooltip: { theme: 'dark', style: { fontSize: '12px' }, x: { show: true } },
        legend: { show: false },
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

function createHorizontalBars() {
    console.log('Creando Horizontal Bars con datos:', permisosPorArea);
    if (!permisosPorArea.length) {
        console.warn('No hay datos para Horizontal Bars');
        document.getElementById('horizontal-bars').innerHTML = '<p class="text-gray-500 text-center">No hay datos disponibles</p>';
        return;
    }
    const margin = { top: 20, right: 30, bottom: 40, left: 150 };
    const width = 400 - margin.left - margin.right;
    const height = 300 - margin.top - margin.bottom;

    const svg = d3.select("#horizontal-bars")
        .append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom)
        .append("g")
        .attr("transform", `translate(${margin.left}, ${margin.top})`);

    const x = d3.scaleLinear()
        .domain([0, d3.max(permisosPorArea, d => d.Value) || 1])
        .range([0, width]);

    const y = d3.scaleBand()
        .domain(permisosPorArea.map(d => d.Name))
        .range([0, height])
        .padding(0.2);

    svg.selectAll(".bar")
        .data(permisosPorArea)
        .enter().append("rect")
        .attr("class", "bar animated-bar")
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

    svg.append("g")
        .selectAll(".label")
        .data(permisosPorArea)
        .enter().append("text")
        .attr("class", "label")
        .attr("x", -10)
        .attr("y", d => y(d.Name) + y.bandwidth() / 2)
        .attr("text-anchor", "end")
        .attr("alignment-baseline", "middle")
        .style("font-size", "12px")
        .style("font-weight", "500")
        .style("fill", "#374151")
        .text(d => d.Name);

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
    if (!permisosPorArea.length) return;
    d3.selectAll(".bar")
        .attr("width", 0)
        .transition()
        .duration(800)
        .delay((d, i) => i * 100)
        .attr("width", d => {
            const x = d3.scaleLinear()
                .domain([0, d3.max(permisosPorArea, d => d.Value) || 1])
                .range([0, 250]);
            return x(d.Value);
        });
}

function createRadialChart() {
    console.log('Creando Radial Chart con datos:', kpiData, 'ConGoce:', conGoce, 'SinGoce:', sinGoce);
    const total = kpiData.TotalPermisos || 0;
    if (total === 0) {
        console.warn('No hay datos para Radial Chart');
        document.getElementById('radial-chart').innerHTML = '<p class="text-gray-500 text-center">No hay datos disponibles</p>';
        return;
    }
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
                endAngle: 270,
                hollow: { margin: 5, size: '30%', background: 'transparent' },
                dataLabels: { name: { show: false }, value: { show: false } }
            }
        },
        colors: ['#10b981', '#f59e0b', '#3b82f6', '#8b5cf6'],
        series: [
            total > 0 ? (kpiData.Aprobados / total * 100).toFixed(1) : 0,
            total > 0 ? (kpiData.Pendientes / total * 100).toFixed(1) : 0,
            total > 0 ? (conGoce / total * 100).toFixed(1) : 0,
            total > 0 ? (sinGoce / total * 100).toFixed(1) : 0
        ],
        labels: ['Aprobados', 'Pendientes', 'Con Goce', 'Sin Goce'],
        legend: { show: true, floating: true, fontSize: '12px', position: 'left', offsetX: 160, offsetY: 15 }
    };
    const radialChart = new ApexCharts(document.querySelector("#radial-chart"), options);
    radialChart.render();
}

function createHeatmap() {
    console.log('Creando Heatmap con datos:', heatmapData);
    if (!heatmapData.length) {
        console.warn('No hay datos para Heatmap');
        document.getElementById('heatmap').innerHTML = '<p class="text-gray-500 text-center">No hay datos disponibles</p>';
        return;
    }
    const margin = { top: 20, right: 20, bottom: 30, left: 60 };
    const cellSize = 60;
    const width = 7 * cellSize + margin.left + margin.right;
    const height = 4 * cellSize + margin.top + margin.bottom;

    const svg = d3.select("#heatmap")
        .append("svg")
        .attr("width", width)
        .attr("height", height)
        .append("g")
        .attr("transform", `translate(${margin.left}, ${margin.top})`);

    const weeks = [...new Set(heatmapData.map(d => d.Week))];
    const days = ['Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb', 'Dom'];

    const colorScale = d3.scaleSequential()
        .interpolator(d3.interpolateBlues)
        .domain([0, d3.max(heatmapData, d => d.Value) || 1]);

    const cells = svg.selectAll(".cell")
        .data(heatmapData)
        .enter().append("rect")
        .attr("class", "cell")
        .attr("x", d => days.indexOf(d.Day) * cellSize)
        .attr("y", d => weeks.indexOf(d.Week) * cellSize)
        .attr("width", cellSize - 2)
        .attr("height", cellSize - 2)
        .attr("rx", 8)
        .style("fill", d => d.Value === 0 ? '#f3f4f6' : colorScale(d.Value))
        .style("stroke", "white")
        .style("stroke-width", 2)
        .style("cursor", "pointer")
        .style("opacity", 0)
        .on("mouseover", function (event, d) {
            d3.select(this).style("stroke", "#3b82f6").style("stroke-width", 3);
            const tooltip = d3.select("body").append("div")
                .attr("class", "tooltip")
                .style("position", "absolute")
                .style("background", "rgba(17, 24, 39, 0.95)")
                .style("color", "white")
                .style("padding", "8px 12px")
                .style("border-radius", "8px")
                .style("font-size", "12px")
                .style("pointer-events", "none")
                .style("opacity", 0);
            tooltip.transition().duration(200).style("opacity", 1);
            tooltip.html(`${d.Week} - ${d.Day}<br/>${d.Value} permisos`)
                .style("left", (event.pageX + 10) + "px")
                .style("top", (event.pageY - 10) + "px");
        })
        .on("mouseout", function (event, d) {
            d3.select(this).style("stroke", "white").style("stroke-width", 2);
            d3.selectAll(".tooltip").remove();
        })
        .transition()
        .duration(800)
        .delay((d, i) => i * 50)
        .style("opacity", 1);

    svg.selectAll(".day-label")
        .data(days)
        .enter().append("text")
        .attr("class", "day-label")
        .attr("x", (d, i) => i * cellSize + cellSize / 2)
        .attr("y", -10)
        .attr("text-anchor", "middle")
        .style("font-size", "12px")
        .style("font-weight", "500")
        .style("fill", "#6b7280")
        .text(d => d);

    svg.selectAll(".week-label")
        .data(weeks)
        .enter().append("text")
        .attr("class", "week-label")
        .attr("x", -10)
        .attr("y", (d, i) => i * cellSize + cellSize / 2)
        .attr("text-anchor", "end")
        .attr("alignment-baseline", "middle")
        .style("font-size", "12px")
        .style("font-weight", "500")
        .style("fill", "#6b7280")
        .text(d => d);

    svg.selectAll(".cell-value")
        .data(heatmapData.filter(d => d.Value > 0))
        .enter().append("text")
        .attr("class", "cell-value")
        .attr("x", d => days.indexOf(d.Day) * cellSize + cellSize / 2)
        .attr("y", d => weeks.indexOf(d.Week) * cellSize + cellSize / 2)
        .attr("text-anchor", "middle")
        .attr("alignment-baseline", "middle")
        .style("font-size", "14px")
        .style("font-weight", "bold")
        .style("fill", "white")
        .style("opacity", 0)
        .text(d => d.Value)
        .transition()
        .duration(800)
        .delay((d, i) => i * 50 + 400)
        .style("opacity", 1);
}

function updateHeatmap() {
    if (!heatmapData.length) return;
    d3.select("#heatmap svg").remove();
    createHeatmap();
}

document.addEventListener('DOMContentLoaded', function () {
    console.log('Inicializando gráficos...');
    createDonutChart();
    createAreaChart();
    createHorizontalBars();
    createRadialChart();
    createHeatmap();

    const kpiCards = document.querySelectorAll('.grid > div');
    kpiCards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'all 0.6s ease-out';
        setTimeout(() => {
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, index * 100);
    });
});