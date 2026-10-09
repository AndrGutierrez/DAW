import type { ReportRow } from './operations';
import { units } from './operations';
import { careText } from './care';
export type ExportInfo = { kind: 'clinical' | 'production'; from: string; to: string; scope: string; generatedAt: string };
const headers = ['Fecha', 'Finca', 'Animal', 'Registro', 'Producto (catálogo actual)', 'Cantidad', 'Unidad', 'Retiro hasta (inclusive)', 'Detalle', 'Observaciones', 'ID'];
export function reportCells(row: ReportRow): (string | number | null)[] { return [row.date, row.farm, row.animal, careText(row.kind), row.product, row.quantity, row.unit ? units[row.unit] || careText(row.unit) : null, row.withdrawalEndDate, row.detail ? careText(row.detail) : null, row.notes, row.id]; }
function download(blob: Blob, name: string) { const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = name; link.click(); setTimeout(() => URL.revokeObjectURL(url), 30000); }
const title = (info: ExportInfo) => info.kind === 'clinical' ? 'Historial clínico' : 'Producción animal';
export async function exportXlsx(rows: ReportRow[], info: ExportInfo) {
  const ExcelJS = (await import('exceljs')).default; const workbook = new ExcelJS.Workbook(); workbook.creator = 'Gestión ganadera'; workbook.created = new Date(info.generatedAt);
  const sheet = workbook.addWorksheet('Registros', { views: [{ state: 'frozen', ySplit: 5, showGridLines: false }], pageSetup: { orientation: 'landscape', fitToPage: true, fitToWidth: 1, fitToHeight: 0, printTitlesRow: '1:5' } });
  sheet.columns = [14, 28, 22, 24, 35, 14, 28, 24, 30, 55, 40].map(width => ({ width }));
  sheet.mergeCells('A1:K1'); sheet.getCell('A1').value = title(info); sheet.getCell('A1').font = { size: 18, bold: true, color: { argb: 'FF2F614C' } }; sheet.getRow(1).height = 30;
  sheet.mergeCells('A2:K2'); sheet.getCell('A2').value = info.scope + ' · ' + info.from + ' a ' + info.to;
  sheet.mergeCells('A3:K3'); sheet.getCell('A3').value = 'Generado: ' + info.generatedAt + ' · Registros: ' + rows.length;
  sheet.mergeCells('A4:K4'); sheet.getCell('A4').value = info.kind === 'clinical' ? 'Dosis sin unidad registrada. El retiro almacenado incluye la última fecha restringida.' : 'Cada fila conserva su unidad. No se suman cantidades de unidades diferentes.';
  const heading = sheet.getRow(5); heading.values = headers; heading.height = 42;
  heading.eachCell(cell => { cell.font = { name: 'Calibri', size: 11, bold: true, color: { argb: 'FFFFFFFF' } }; cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF2F614C' } }; cell.alignment = { vertical: 'middle', wrapText: true }; cell.border = { right: { style: 'thin', color: { argb: 'FFFFFFFF' } } }; });
  for (const [index, source] of rows.entries()) {
    const values: (string | number | Date | null)[] = reportCells(source); values[0] = new Date(source.date + 'T00:00:00Z'); if (source.withdrawalEndDate) values[7] = new Date(source.withdrawalEndDate + 'T00:00:00Z');
    const row = sheet.addRow(values); row.height = Math.min(409, Math.max(32, 16 * (1 + Math.ceil((source.notes?.length || 0) / 52))));
    row.eachCell({ includeEmpty: true }, cell => { cell.font = { name: 'Calibri', size: 11, color: { argb: 'FF24352C' } }; cell.alignment = { vertical: 'top', wrapText: true }; if (index % 2 === 0) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF3F5EF' } }; });
    row.getCell(1).alignment = { vertical: 'top', horizontal: 'left' }; row.getCell(1).numFmt = 'dd/mm/yyyy'; row.getCell(8).numFmt = 'dd/mm/yyyy'; row.getCell(6).numFmt = '#,##0.0000';
  }
  sheet.autoFilter = { from: 'A5', to: 'K' + Math.max(5, rows.length + 5) };
  const buffer = await workbook.xlsx.writeBuffer(); download(new Blob([buffer as ArrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), info.kind + '-' + info.from + '-' + info.to + '.xlsx');
}
export async function exportPdf(rows: ReportRow[], info: ExportInfo) {
  const [{ jsPDF }, { autoTable }] = await Promise.all([import('jspdf'), import('jspdf-autotable')]);
  const response = await fetch('/fonts/NotoSans-Regular.ttf'); if (!response.ok) throw new Error('No se pudo cargar la fuente del reporte. Reintenta la exportación.');
  const bytes = new Uint8Array(await response.arrayBuffer()); let binary = ''; for (let i = 0; i < bytes.length; i += 8192) binary += String.fromCharCode(...bytes.subarray(i, i + 8192));
  const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a3' }); doc.addFileToVFS('NotoSans.ttf', btoa(binary)); doc.addFont('NotoSans.ttf', 'NotoSans', 'normal'); doc.setFont('NotoSans');
  autoTable(doc, { head: [headers.slice(0, 10)], body: rows.map(row => reportCells(row).slice(0, 10).map(v => v === null ? '' : String(v))), startY: 39, margin: { top: 39, bottom: 16, left: 12, right: 12 }, styles: { font: 'NotoSans', fontStyle: 'normal', fontSize: 8, cellPadding: 2, overflow: 'linebreak' }, headStyles: { fillColor: [47, 97, 76], textColor: 255, fontStyle: 'normal' }, alternateRowStyles: { fillColor: [243, 245, 239] }, columnStyles: { 0: { cellWidth: 23 }, 1: { cellWidth: 32 }, 2: { cellWidth: 29 }, 3: { cellWidth: 34 }, 4: { cellWidth: 48 }, 5: { cellWidth: 22 }, 6: { cellWidth: 38 }, 7: { cellWidth: 27 }, 8: { cellWidth: 35 }, 9: { cellWidth: 84 } }, showHead: 'everyPage', rowPageBreak: 'avoid', didDrawPage: () => { doc.setFont('NotoSans'); doc.setFontSize(16); doc.setTextColor(47, 97, 76); doc.text(title(info), 12, 14); doc.setFontSize(9); doc.setTextColor(65); doc.text(doc.splitTextToSize(info.scope + ' · ' + info.from + ' a ' + info.to + ' · ' + rows.length + ' registros', 390), 12, 22); doc.text('Generado: ' + info.generatedAt, 12, 28); doc.text(info.kind === 'clinical' ? 'Dosis sin unidad registrada. El retiro incluye la última fecha restringida.' : 'Cada fila conserva su unidad; no se suman unidades diferentes.', 12, 34); } });
  const pages = doc.getNumberOfPages(); for (let p = 1; p <= pages; p++) { doc.setPage(p); doc.setFont('NotoSans'); doc.setFontSize(9); doc.text('Página ' + p + ' de ' + pages, 12, 289); }
  doc.save(info.kind + '-' + info.from + '-' + info.to + '.pdf');
}
