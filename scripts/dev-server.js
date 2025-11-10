const http = require('http');
const port = process.env.PORT ? Number(process.env.PORT) : 8080;

const server = http.createServer((req, res) => {
  if (req.url === '/' || req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('OK');
    return;
  }
  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('Not Found');
});

server.listen(port, '127.0.0.1', () => {
  console.log(`Dev server listening on http://127.0.0.1:${port}`);
});
