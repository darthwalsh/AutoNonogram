import time
from com.android.monkeyrunner import MonkeyRunner, MonkeyDevice
from BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer

class PostHandler(BaseHTTPRequestHandler):
  def do_GET(self):
    self.send_response(200)
    self.end_headers()

  def do_POST(self):
    content_len = int(self.headers.getheader('content-length', 0))
    post_body = self.rfile.read(content_len)
    for pair in post_body.split(';'):
      x, y = map(int, pair.split(','))
      device.touch(x, y, MonkeyDevice.DOWN_AND_UP)
      time.sleep(0.08)
    
    self.send_response(200)
    self.end_headers()

if __name__ == '__main__':
  print 'Waiting for device connection...'
  device = MonkeyRunner.waitForConnection()

  server = HTTPServer(('localhost', 8080), PostHandler)
  print 'Got device, starting server, use <Ctrl-C> to stop'
  server.serve_forever()
