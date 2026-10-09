export async function consumeAnalyticsEvents(response: Response, onChanged: () => void, onReady: () => void) {
  if (!response.headers.get('Content-Type')?.includes('text/event-stream') || !response.body)
    throw new Error('The analytics event stream is unavailable.');
  const reader = response.body.getReader(); const decoder = new TextDecoder(); let pending = '';
  try {
    while (true) {
      const chunk = await reader.read(); if (chunk.done) break;
      pending += decoder.decode(chunk.value, { stream: true });
      let match: RegExpExecArray | null;
      while ((match = /\r?\n\r?\n/.exec(pending))) {
        const frame = pending.slice(0, match.index); pending = pending.slice(match.index + match[0].length);
        const event = frame.split(/\r?\n/).find(line => line.startsWith('event:'))?.slice(6).trim();
        if (event === 'ready') onReady(); else if (event === 'changed') onChanged();
      }
      if (pending.length > 8192) throw new Error('Invalid analytics event frame.');
    }
  } finally { await reader.cancel().catch(() => {}); reader.releaseLock(); }
}
