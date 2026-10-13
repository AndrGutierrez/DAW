const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const model = JSON.parse(fs.readFileSync(path.join(root, 'db/diagram/schema-model.json'), 'utf8'));
const tick = String.fromCharCode(96);
const text = value => String(value ?? '').replaceAll('|', '\\|').replaceAll('\n', ' ');
const lines = ['# Relational data dictionary', '', 'Generated from the migrated PostgreSQL schema. Regenerate the diagrams first, then run node scripts/erd/data-dictionary.cjs.', '',
  'Source: ' + tick + 'db/schema.sql' + tick + '; SHA-256: ' + tick + model.sourceSha256 + tick + '.', '',
  'Every table below is present in the physical schema. A modeled table does not imply that its complete business workflow has been implemented.', ''];
for (const table of model.tables) {
  lines.push('## ' + table.name, '', '| Column | PostgreSQL type | Nullable | Key / constraint | Default |', '| --- | --- | --- | --- | --- |');
  for (const column of table.columns) {
    const rules = [];
    if (table.primaryKey.includes(column.name)) rules.push('PK');
    for (const fk of model.relationships.filter(fk => fk.child === table.name && fk.columns.includes(column.name))) rules.push('FK: ' + fk.parent + '(' + fk.parentColumns.join(', ') + ')');
    for (const index of table.uniqueIndexes.filter(index => index.columns.includes(column.name))) rules.push('UNIQUE: ' + index.name + (index.condition ? ' [' + index.condition + ']' : ''));
    const defaultValue = column.definition.match(/DEFAULT (.*?)(?: NOT NULL|$)/)?.[1] || '';
    lines.push('| ' + [column.name, column.type, column.nullable ? 'Yes' : 'No', rules.join('; '), defaultValue].map(text).join(' | ') + ' |');
  }
  lines.push('');
  for (const check of table.checks || []) lines.push('- CHECK: ' + tick + text(typeof check === 'string' ? check : JSON.stringify(check)) + tick);
  for (const fk of model.relationships.filter(fk => fk.child === table.name)) lines.push('- ' + tick + fk.name + tick + ': ' + fk.columns.join(', ') + ' -> ' + fk.parent + '(' + fk.parentColumns.join(', ') + '); delete behavior: ' + tick + (fk.deleteBehavior || 'NO ACTION') + tick + '.');
  lines.push('');
}
fs.writeFileSync(path.join(root, 'db/data-dictionary.md'), lines.join('\n').trimEnd() + '\n', 'utf8');
console.log('Generated a dictionary for ' + model.tables.length + ' tables and ' + model.tables.reduce((sum, t) => sum + t.columns.length, 0) + ' columns.');
