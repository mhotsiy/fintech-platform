import { test as base, ConsoleMessage } from '@playwright/test';

type ConsoleEntry = {
  type: string;
  text: string;
  timestamp: number;
};

export const test = base.extend<{ consoleErrors: string[] }>({
  consoleErrors: async ({}, use) => {
    await use([]);
  },
  
  page: async ({ page }, use, testInfo) => {
    const messages: ConsoleEntry[] = [];
    const errors: ConsoleEntry[] = [];
    const warnings: ConsoleEntry[] = [];

    const handleConsole = (msg: ConsoleMessage) => {
      const type = msg.type();
      const text = msg.text();
      const entry = { type, text, timestamp: Date.now() };
      
      switch (type) {
        case 'error':
          errors.push(entry);
          console.error(`Console Error: ${text}`);
          break;
        case 'warning':
          warnings.push(entry);
          console.warn(`Console Warning: ${text}`);
          break;
        case 'log':
        case 'info':
          messages.push(entry);
          break;
      }
    };

    const handlePageError = (error: Error) => {
      const entry = {
        type: 'pageerror',
        text: `${error.message}\n${error.stack}`,
        timestamp: Date.now()
      };
      errors.push(entry);
      console.error(`Uncaught Error: ${error.message}`);
    };

    const handleRequestFailed = (request: any) => {
      const entry = {
        type: 'requestfailed',
        text: `${request.url()} - ${request.failure()?.errorText || 'Unknown'}`,
        timestamp: Date.now()
      };
      errors.push(entry);
      console.error(`Failed Request: ${request.url()}`);
    };

    page.on('console', handleConsole);
    page.on('pageerror', handlePageError);
    page.on('requestfailed', handleRequestFailed);

    await use(page);

    // Cleanup listeners to prevent memory leaks
    page.off('console', handleConsole);
    page.off('pageerror', handlePageError);
    page.off('requestfailed', handleRequestFailed);

    // Attach artifacts only if present
    if (errors.length > 0) {
      console.log(`\n${errors.length} console error(s) detected`);
      testInfo.attach('console-errors', {
        body: errors.map(e => `[${e.type}] ${e.text}`).join('\n\n'),
        contentType: 'text/plain',
      });
    }

    if (warnings.length > 0) {
      testInfo.attach('console-warnings', {
        body: warnings.map(w => w.text).join('\n'),
        contentType: 'text/plain',
      });
    }

    // Only attach verbose logs if test failed
    if (testInfo.status === 'failed' && messages.length > 0) {
      testInfo.attach('console-logs', {
        body: messages.map(m => `[${m.type}] ${m.text}`).join('\n'),
        contentType: 'text/plain',
      });
    }
  },
});

export { expect } from '@playwright/test';
