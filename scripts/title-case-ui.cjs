// Mechanical source-only label formatting. Run with --check for CI or --write to apply.
// Deliberately excludes Razor expressions, scripts, styles, prose and implicit option values.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const acronyms = new Set('ID PMS POS AR AP VAT BIR TIN PDF CSV UI UX QA AI API URL SSS HDMF PHIC PHP USD KPI SLA MFA OTP IP SMS SMTP TLS SSL HR FIFO FEFO JSON XML SQL ETA ETD'.split(' '));
function title(text) {
  return text.replace(/&(?:#\d+|#x[\da-f]+|\w+);|[A-Za-z]+(?:'[A-Za-z]+)?/gi, word => {
    if (word.startsWith('&')) return word;
    if (/^(ids|kpis|urls)$/i.test(word)) return word.slice(0, -1).toUpperCase() + 's';
    return acronyms.has(word.toUpperCase()) || (word.length > 1 && word === word.toUpperCase()) ? word.toUpperCase() : word[0].toUpperCase() + word.slice(1).toLowerCase();
  });
}
function isLabel(text) {
  const value = text.trim();
  return value.length > 0 && value.length <= 110 && !/[@{}=\\]|https?:|[.!?]$/.test(value) && !value.includes('\n') && value.split(/\s+/).length <= 12;
}
function format(source) {
  const protectedParts = [];
  source = source.replace(/<(script|style|pre|textarea)\b[\s\S]*?<\/\1\s*>|@\*[\s\S]*?\*@/gi, block => `\u0000${protectedParts.push(block) - 1}\u0000`);
  source = source.replace(/<(h[1-6]|label|th|dt|button|a|summary|legend|option)\b((?:"[^"]*"|'[^']*'|[^'">])*)>([\s\S]*?)<\/\1\s*>/gi, (whole, tag, attrs, inner) => {
    if (tag.toLowerCase() === 'option' && !/\bvalue\s*=/.test(attrs)) return whole;
    // Work on plain text nodes, not attributes or code inside Razor control blocks.
    const contents = inner.split(/(<[^>]+>)/g).map(part => !part.startsWith('<') && isLabel(part) ? title(part) : part).join('');
    return `<${tag}${attrs}>${contents}</${tag}>`;
  });
  source = source.replace(/<(span|strong|small|div|p)\b([^>]*?)>([^<>]*?)<\/\1\s*>/gi, (whole, tag, attrs, inner) => {
    // Short, non-sentence captions are labels. Free text expressions remain untouched.
    if (!isLabel(inner) || (tag === 'p' || tag === 'div') && !/(kicker|eyebrow|label|caption|breadcrumb|text-muted small)/i.test(attrs)) return whole;
    return `<${tag}${attrs}>${title(inner)}</${tag}>`;
  });
  source = source.replace(/((?:aria-label|(?<![\w-])title|data-vpms-(?:native-)?dialog-title|placeholder)\s*=\s*")([^"@]*)(")/gi, (whole, before, value, after) => isLabel(value) && !/^e\.g\./i.test(value) ? before + title(value) + after : whole);
  source = source.replace(/(ViewData\["Title"\]\s*=\s*")([^"@]*)(")/g, (whole, before, value, after) => before + title(value) + after);
  source = source.replaceAll('Html.GetEnumSelectList<', 'Html.GetUiEnumSelectList<');
  source = source.replace(/@Model\.(NativeActionTitle|NativeActionButtonText)\b/g, '@UiText.Label(Model.$1)');
  source = source.replace(/>@([A-Za-z_]\w*(?:\??\.[A-Za-z_]\w*)*\.(?:\w*Status|Priority|RefundMethod|PaymentMethod))\s*</g, '>@UiText.Display($1)<');
  return source.replace(/\u0000(\d+)\u0000/g, (_, index) => protectedParts[Number(index)]);
}
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? files(full) : entry.name.endsWith('.cshtml') ? [full] : [];
  });
}
let changed = 0;
// Safety checks also run during CI's --check. Never rewrite data or binding values.
const assert = require('node:assert/strict');
assert.equal(format('<button disabled="@(count > 0)">Housekeeping task</button>'), '<button disabled="@(count > 0)">Housekeeping Task</button>');
assert.equal(format('<span>@guest.LastName</span>'), '<span>@guest.LastName</span>');
assert.equal(format('<option>CreditCard</option>'), '<option>CreditCard</option>');
assert.equal(format('<option value="CreditCard">Credit card</option>'), '<option value="CreditCard">Credit Card</option>');
assert.equal(format('<textarea>de la Cruz</textarea>'), '<textarea>de la Cruz</textarea>');
assert.equal(format('<h2>Housekeeping &amp; room readiness</h2>'), '<h2>Housekeeping &amp; Room Readiness</h2>');
for (const file of [...files(path.join(root, 'Pages')), ...files(path.join(root, 'Areas'))]) {
  const before = fs.readFileSync(file, 'utf8');
  const after = format(before);
  if (before === after) continue;
  changed++;
  console.log(path.relative(root, file));
  if (process.argv.includes('--write')) fs.writeFileSync(file, after, 'utf8');
}
console.log(`${changed} UI files ${process.argv.includes('--write') ? 'updated' : 'need label formatting'}.`);
if (changed && process.argv.includes('--check')) process.exitCode = 1;
