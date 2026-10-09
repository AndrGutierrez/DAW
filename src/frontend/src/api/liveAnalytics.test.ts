import { describe, it, expect } from 'vitest';
import { consumeAnalyticsEvents } from './liveAnalytics';
function response(parts: string[], contentType = 'text/event-stream') {
  return new Response(new ReadableStream<Uint8Array>({ start(controller) { for (const part of parts) controller.enqueue(new TextEncoder().encode(part)); controller.close(); } }), { headers: { 'Content-Type': contentType } });
}
describe('authenticated analytics event parser', () => {
  it('handles frames split across network chunks and ignores heartbeat', async () => {
    const events: string[] = [];
    await consumeAnalyticsEvents(response(['event: rea', 'dy\ndata: 0\n\nevent: heartbeat\ndata: 0\n\nevent: chan', 'ged\ndata: 1\n\n']), () => events.push('changed'), () => events.push('ready'));
    expect(events).toEqual(['ready', 'changed']);
  });
  it('accepts CRLF boundaries across chunks and multiple notifications', async () => {
    let changed = 0;
    await consumeAnalyticsEvents(response(['event: changed\r', '\ndata: 1\r\n\r', '\nevent: changed\r\ndata: 2\r\n\r\n']), () => changed++, () => {});
    expect(changed).toBe(2);
  });
  it('does not treat JSON error responses as a working live connection', async () => {
    await expect(consumeAnalyticsEvents(response(['{}'], 'application/problem+json'), () => {}, () => {})).rejects.toThrow('unavailable');
  });
  it('rejects an oversized unfinished frame instead of accumulating unbounded memory', async () => {
    await expect(consumeAnalyticsEvents(response(['x'.repeat(8193)]), () => {}, () => {})).rejects.toThrow('Invalid analytics');
  });
});
