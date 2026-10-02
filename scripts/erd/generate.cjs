const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

// DAW_ERD_MODULES optionally points to an existing dependency installation.
const load = name => require(require.resolve(name, {
  paths: [process.env.DAW_ERD_MODULES || __dirname]
}));
const { instance } = load('@viz-js/viz');
const sharp = load('sharp');
const root = path.resolve(__dirname, '../..');
const output = path.join(root, 'db/diagram');
const sql = fs.readFileSync(path.join(root, 'db/schema.sql'), 'utf8');
const sourceHash = crypto.createHash('sha256').update(sql.replace(/\r\n/g, '\n')).digest('hex');
const quote = value => JSON.stringify(value);
const escape = value => String(value).replace(/[&<>"']/g, character => ({
  '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
}[character]));
const identifiers = text => [...text.matchAll(/"([^"]+)"/g)].map(match => match[1]);
const tables = new Map();

// Parse the pg_dump form used in schema.sql; fail on unsupported statements.
for (const match of sql.matchAll(/^CREATE TABLE public\."([^"]+)" \(\r?\n([\s\S]*?)^\);/gm)) {
  const columns = [];
  const checks = [];
  for (const line of match[2].split(/\r?\n/)) {
    const column = line.match(/^\s+"([^"]+)" (.+?)(?:,)?$/);
    if (column) {
      const definition = column[2].replace(/,$/, '');
      const type = definition.split(/ DEFAULT | NOT NULL| GENERATED /)[0];
      columns.push({ name: column[1], type, nullable: !definition.includes('NOT NULL'), definition });
    } else if (line.trim()) {
      if (!/^CONSTRAINT .* CHECK /.test(line.trim())) throw new Error(`Unsupported column: ${line}`);
      checks.push(line.trim().replace(/,$/, ''));
    }
  }
  tables.set(match[1], { name: match[1], columns, checks, primaryKey: [], uniqueIndexes: [] });
}
const declaredTableCount = (sql.match(/^CREATE TABLE /gm) || []).length;
if (tables.size !== declaredTableCount) throw new Error('Some CREATE TABLE statements were not parsed');

for (const match of sql.matchAll(/ALTER TABLE ONLY public\."([^"]+)"\s+ADD CONSTRAINT "([^"]+)" PRIMARY KEY \(([^;]+)\);/g)) {
  tables.get(match[1]).primaryKey = identifiers(match[3]);
}
for (const match of sql.matchAll(/CREATE UNIQUE INDEX "([^"]+)" ON public\."([^"]+)" USING btree \((.+?)\)([^;]*);/g)) {
  tables.get(match[2]).uniqueIndexes.push({ name: match[1], columns: identifiers(match[3]), condition: match[4].trim() });
}
const relationships = [];
for (const match of sql.matchAll(/ALTER TABLE ONLY public\."([^"]+)"\s+ADD CONSTRAINT "([^"]+)" FOREIGN KEY \(([^)]+)\) REFERENCES public\."([^"]+)"\(([^)]+)\)([^;]*);/g)) {
  const child = tables.get(match[1]);
  const parent = tables.get(match[4]);
  const columns = identifiers(match[3]);
  const parentColumns = identifiers(match[5]);
  if (!child || !parent || columns.some(name => !child.columns.some(column => column.name === name)) || parentColumns.some(name => !parent.columns.some(column => column.name === name))) {
    throw new Error(`Invalid foreign key ${match[2]}`);
  }
  const nullable = columns.some(name => child.columns.find(column => column.name === name).nullable);
  const singleChild = [child.primaryKey, ...child.uniqueIndexes.filter(index => !index.condition).map(index => index.columns)]
    .some(key => key.length && key.every(name => columns.includes(name)));
  relationships.push({ name: match[2], child: child.name, parent: parent.name, columns, parentColumns,
    nullable, parentCardinality: nullable ? '0..1' : '1', childCardinality: singleChild ? '0..1' : '0..N',
    deleteBehavior: match[6].match(/ON DELETE ([A-Z ]+)/)?.[1].trim() || 'NO ACTION' });
}
if (relationships.length !== (sql.match(/FOREIGN KEY \(/g) || []).length || [...tables.values()].some(table => !table.primaryKey.length)) {
  throw new Error('Schema contains unparsed constraints');
}

const modules = [
  { name: 'Territorio y animales', color: '#17634d', light: '#edf7f1', tables: ['Farms', 'Paddocks', 'Lots', 'Species', 'Breeds', 'Animals', 'AnimalMovements', 'AnimalPhotos', 'HealthStatusChanges', 'WeightRecords', 'AnimalProduction'] },
  { name: 'Insumos y alimentación', color: '#246b9e', light: '#edf5fc', tables: ['InventoryCategories', 'Products', 'FarmInventory', 'Suppliers', 'ProductBatches', 'StockMovements', 'Rations', 'RationIngredients', 'FeedingRecords'] },
  { name: 'Salud y reproducción', color: '#9b5832', light: '#fff5eb', tables: ['Diseases', 'HealthEvents', 'SemenBatches', 'ReproductiveEvents'] },
  { name: 'Operación y trazabilidad', color: '#7e638f', light: '#f7f0fb', tables: ['Tasks', 'Transactions', 'Alerts', 'AlertRules', 'Attachments', 'AuditLogs'] },
  { name: 'Identidad y acceso', color: '#4155a2', light: '#f0f2fe', tables: ['Users', 'Roles', 'Permissions', 'UserRoles', 'UserPermissions', 'RolePermissions', 'UserFarms', 'UserClaims', 'RoleClaims', 'UserLogins', 'UserTokens', 'RefreshTokens'] },
  { name: 'Historial técnico', color: '#687788', light: '#f1f4f7', tables: ['__EFMigrationsHistory'] }
];
const moduleFor = name => modules.find(module => module.tables.includes(name));
if ([...tables.keys()].some(name => !moduleFor(name))) throw new Error('Assign each new table to a diagram module');
const coreNames = ['Farms', 'Species', 'Breeds', 'Paddocks', 'Lots', 'Animals', 'AnimalPhotos', 'HealthStatusChanges', 'WeightRecords', 'AnimalProduction', 'InventoryCategories', 'Products', 'FarmInventory'];
const coreFields = {
  Farms: ['Name', 'Code', 'IsActive'], Species: ['Name', 'Code', 'Purpose'], Breeds: ['Name', 'Purpose'],
  Paddocks: ['Name', 'AreaHectares', 'Capacity'], Lots: ['Name', 'Purpose'],
  Animals: ['InternalTag', 'BirthDate', 'Sex', 'Status', 'HealthStatus', 'Purpose'],
  AnimalPhotos: ['FileName', 'Url'], HealthStatusChanges: ['PreviousStatus', 'NewStatus', 'Reason'],
  WeightRecords: ['Date', 'WeightKg'], AnimalProduction: ['OperationId', 'Date', 'ProductType', 'Method', 'Quantity', 'Unit'],
  InventoryCategories: ['Name', 'IsActive'], Products: ['SKU', 'Name', 'Price', 'CostPrice', 'Unit', 'Brand', 'IsActive'],
  FarmInventory: ['Stock', 'MinStock', 'MaxStock', 'Location']
};
const foreignColumns = table => relationships.filter(relation => relation.child === table.name).flatMap(relation => relation.columns);
const typeLabel = type => type.replace('character varying', 'varchar').replace('timestamp with time zone', 'timestamptz').replace('integer', 'int').replace('boolean', 'bool').replace('double precision', 'float8');

function nodeLabel(table, compact) {
  const module = moduleFor(table.name);
  const fks = foreignColumns(table);
  const columns = compact ? table.columns.filter(column => table.primaryKey.includes(column.name) || fks.includes(column.name) || coreFields[table.name]?.includes(column.name)) : table.columns;
  const rows = columns.map((column, index) => {
    const badges = [table.primaryKey.includes(column.name) ? 'PK' : '', fks.includes(column.name) ? 'FK' : '', table.uniqueIndexes.some(key => key.columns.length === 1 && key.columns[0] === column.name) ? 'UQ' : ''].filter(Boolean).join(' ');
    return `<TR><TD ALIGN="LEFT" BGCOLOR="${index % 2 ? '#f8fafb' : '#ffffff'}" PORT="${escape(column.name)}"><FONT COLOR="${badges ? module.color : '#758295'}" POINT-SIZE="9">${badges || '·'}</FONT>  ${escape(column.name)}${column.nullable ? ' ?' : ''}</TD><TD ALIGN="RIGHT" BGCOLOR="${index % 2 ? '#f8fafb' : '#ffffff'}"><FONT COLOR="#637187" POINT-SIZE="10">${escape(typeLabel(column.type))}</FONT></TD></TR>`;
  }).join('');
  return `<TABLE BORDER="1" COLOR="#d5dfe5" CELLBORDER="0" CELLSPACING="0" CELLPADDING="6"><TR><TD COLSPAN="2" BGCOLOR="${module.color}" ALIGN="LEFT" PORT="header"><FONT COLOR="white" POINT-SIZE="16"><B>${escape(table.name)}</B></FONT></TD></TR>${rows}</TABLE>`;
}

function graph(names, compact) {
  const selected = new Set(names);
  const lines = ['digraph DER {', 'graph [rankdir=LR, bgcolor="#f8faf9", pad="0.35", nodesep="0.38", ranksep="1.05", splines=spline, newrank=true, fontname="Arial"];',
    'node [shape=plain, fontname="Arial", fontsize=12];', 'edge [fontname="Arial", fontsize=9, color="#9bafbd", fontcolor="#637187", penwidth=1.1, arrowsize=0.7];'];
  if (!compact) {
    for (const [index, module] of modules.entries()) {
      lines.push(`subgraph cluster_${index} { label=${quote(module.name)}; style="rounded,filled"; fillcolor=${quote(module.light)}; color="#dce5e8"; fontcolor=${quote(module.color)}; fontsize=18; margin=22;`);
      for (const name of module.tables.filter(name => selected.has(name))) lines.push(`${quote(name)} [id=${quote('table-' + name)}, tooltip=${quote(name)}, label=<${nodeLabel(tables.get(name), false)}>];`);
      lines.push('}');
    }
  } else {
    for (const name of names) lines.push(`${quote(name)} [id=${quote('table-' + name)}, tooltip=${quote(name)}, label=<${nodeLabel(tables.get(name), true)}>];`);
  }
  relationships.filter(relation => selected.has(relation.child) && selected.has(relation.parent)).forEach((relation, index) => {
    lines.push(`${quote(relation.parent)}:header:e -> ${quote(relation.child)}:${quote(relation.columns[0])}:w [id=${quote('relation-' + index)}, tooltip=${quote(relation.name + ' · ON DELETE ' + relation.deleteBehavior)}, dir=both, arrowtail=${quote(relation.nullable ? 'teeodot' : 'tee')}, arrowhead=${quote(relation.childCardinality === '0..N' ? 'crowodot' : 'teeodot')}, constraint=${relation.parent === relation.child ? 'false' : 'true'}];`);
  });
  lines.push('}');
  return lines.join('\n');
}

function decorate(svg, title, subtitle) {
  const dimensions = svg.match(/viewBox="[^"]*?([\d.]+) ([\d.]+)"/);
  if (!dimensions) throw new Error('SVG dimensions missing');
  const width = Number(dimensions[1]);
  const height = Number(dimensions[2]);
  const inner = svg.slice(svg.indexOf('>', svg.indexOf('<svg')) + 1, svg.lastIndexOf('</svg>'));
  return `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${width}" height="${height + 145}" viewBox="0 0 ${width} ${height + 145}" role="img" aria-label="${escape(title)}"><rect width="100%" height="100%" fill="#f8faf9"/><g font-family="Arial, sans-serif"><rect x="30" y="27" width="6" height="71" rx="3" fill="#17634d"/><text x="52" y="43" font-size="12" letter-spacing="2" fill="#17634d">GESTIÓN GANADERA</text><text x="52" y="74" font-size="27" font-weight="700" fill="#1b3440">${escape(title)}</text><text x="52" y="97" font-size="12" fill="#637187">${escape(subtitle)}</text><text x="52" y="121" font-size="11" fill="#637187">PK: clave primaria · FK: clave foránea · UQ: único individual · ?: admite NULL · ○: cero · |: uno · pata de cuervo: muchos</text></g><g transform="translate(0 145)">${inner}</g></svg>\n`;
}

async function main() {
  fs.mkdirSync(output, { recursive: true });
  const viz = await instance();
  const fullDot = graph([...tables.keys()], false);
  const coreDot = graph(coreNames, true);
  const coreRelations = relationships.filter(relation => coreNames.includes(relation.child) && coreNames.includes(relation.parent)).length;
  const fullSvg = decorate(viz.renderString(fullDot, { format: 'svg' }), 'Diagrama entidad–relación completo', `${tables.size} tablas · ${relationships.length} claves foráneas · PostgreSQL 15 · columnas y relaciones del esquema físico`);
  const coreSvg = decorate(viz.renderString(coreDot, { format: 'svg' }), 'Núcleo operativo ganadero', `${coreNames.length} tablas · ${coreRelations} claves foráneas · vista resumida de columnas · diagrama completo disponible en el visor`);
  fs.writeFileSync(path.join(output, 'der-completo.svg'), fullSvg);
  fs.writeFileSync(path.join(output, 'der-nucleo.svg'), coreSvg);
  fs.writeFileSync(path.join(output, 'der-completo.dot'), fullDot + '\n');
  await sharp(Buffer.from(coreSvg), { limitInputPixels: false }).resize({ width: 3200, height: 4200, fit: 'inside' }).png().toFile(path.join(output, 'der-nucleo.png'));
  await sharp(Buffer.from(fullSvg), { limitInputPixels: false }).resize({ width: 6400, height: 6400, fit: 'inside' }).png().toFile(path.join(output, 'der-completo.png'));
  const model = { source: 'db/schema.sql', sourceSha256: sourceHash, tableCount: tables.size, foreignKeyCount: relationships.length,
    modules, coreTables: coreNames, tables: [...tables.values()], relationships };
  fs.writeFileSync(path.join(output, 'schema-model.json'), JSON.stringify(model, null, 2) + '\n');
  const template = fs.readFileSync(path.join(__dirname, 'viewer.html'), 'utf8');
  fs.writeFileSync(path.join(output, 'index.html'), template.replace('<!-- CORE_SVG -->', coreSvg).replace('<!-- FULL_SVG -->', fullSvg).replace('/* SCHEMA_DATA */', JSON.stringify(model).replace(/</g, '\\u003c')));
  console.log(`Generated ${tables.size} tables and ${relationships.length} foreign keys in db/diagram`);
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
