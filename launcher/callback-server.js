import http from 'node:http'
import https from 'node:https'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const CLIENT_ID = '1524146338403713085'
const CLIENT_SECRET = 'YeRCIBJnAFZ_uPJY3IKwkCEF16v2WjT0'
const REDIRECT_URI = 'http://localhost:28482/callback'

function exchangeCode(code) {
  return new Promise((resolve, reject) => {
    const data = new URLSearchParams({
      client_id: CLIENT_ID,
      client_secret: CLIENT_SECRET,
      grant_type: 'authorization_code',
      code,
      redirect_uri: REDIRECT_URI,
    }).toString()

    const req = https.request(
      'https://discord.com/api/oauth2/token',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Content-Length': Buffer.byteLength(data),
        },
      },
      (res) => {
        let body = ''
        res.on('data', (chunk) => (body += chunk))
        res.on('end', () => {
          try {
            resolve(JSON.parse(body))
          } catch (e) {
            reject(e)
          }
        })
      }
    )
    req.on('error', reject)
    req.write(data)
    req.end()
  })
}

function getUser(token) {
  return new Promise((resolve, reject) => {
    const req = https.request(
      'https://discord.com/api/users/@me',
      {
        method: 'GET',
        headers: { Authorization: `Bearer ${token}` },
      },
      (res) => {
        let body = ''
        res.on('data', (chunk) => (body += chunk))
        res.on('end', () => {
          try {
            resolve(JSON.parse(body))
          } catch (e) {
            reject(e)
          }
        })
      }
    )
    req.on('error', reject)
    req.end()
  })
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://localhost:28482`)

  if (url.pathname === '/callback') {
    const code = url.searchParams.get('code')
    if (!code) {
      res.writeHead(400, { 'Content-Type': 'text/html; charset=utf-8' })
      res.end('<h1>Hata: Kod bulunamadi</h1>')
      return
    }

    try {
      const tokenData = await exchangeCode(code)
      const user = await getUser(tokenData.access_token)

      // Frontend'e user bilgisi ile don
      const userData = JSON.stringify({ user, token: tokenData.access_token })
      const html = `
        <!DOCTYPE html>
        <html>
        <head><title>Giris Yapiliyor...</title></head>
        <body style="background:#13111c;color:white;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;">
          <script>
            window.opener.postMessage(${userData}, '*');
            window.close();
            document.body.innerHTML = '<h2>Giris basarili! Pencereyi kapatabilirsiniz.</h2>';
          </script>
        </body>
        </html>
      `
      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' })
      res.end(html)
    } catch (err) {
      console.error('OAuth hatasi:', err)
      res.writeHead(500, { 'Content-Type': 'text/html; charset=utf-8' })
      res.end('<h1>Giris hatasi olustu</h1>')
    }
  } else {
    res.writeHead(404)
    res.end('Not found')
  }
})

server.listen(28482, () => {
  console.log('DLI OAuth callback server: http://localhost:28482')
})
