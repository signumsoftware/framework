
export async function* jsonObjectStream<T>(
  reader: ReadableStreamDefaultReader<Uint8Array>
): AsyncGenerator<T> {
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { value, done } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    let newlineIndex: number;
    while ((newlineIndex = buffer.indexOf('\n')) !== -1) {
      const line = buffer.slice(0, newlineIndex).trim();
      buffer = buffer.slice(newlineIndex + 1);

      if (line) {
        try {
          yield JSON.parse(line) as T;
        } catch (err) {
          console.error('Failed to parse JSON line:', line, err);
          // Optionally: throw err or continue
        }
      }
    }
  }

  // Handle any trailing JSON object after the last newline
  const last = buffer.trim();
  if (last) {
    try {
      yield JSON.parse(last) as T;
    } catch (err) {
      console.error('Failed to parse final JSON object:', last, err);
    }
  }
}
