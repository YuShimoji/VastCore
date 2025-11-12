const http = require('http');

function request(url){
  return new Promise((resolve,reject)=>{
    const req = http.get(url, res => {
      let data='';
      res.on('data', c=> data+=c);
      res.on('end', ()=> resolve({ status: res.statusCode, body: data }));
    });
    req.on('error', reject);
  });
}

(async () => {
  try {
    const res = await request('http://127.0.0.1:8080/');
    if (res.status !== 200 || !/OK/.test(res.body)){
      console.error('Smoke check failed:', res.status, res.body);
      process.exit(1);
    }
    console.log('Smoke OK');
  } catch (e) {
    console.error('Smoke exception:', e.message);
    process.exit(1);
  }
})();
