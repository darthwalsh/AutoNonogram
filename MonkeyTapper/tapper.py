import os
import time
from BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer
from com.android.monkeyrunner import MonkeyRunner, MonkeyDevice
from threading import Thread

def current_time_ms():
  return time.time() * 1000

last_used = current_time_ms()

class PostHandler(BaseHTTPRequestHandler):
  def bookkeeping(self):
    global last_used
    last_used = current_time_ms()

    self.send_response(200)
    self.end_headers()


  def do_GET(self):
    self.bookkeeping()

  def do_POST(self):
    self.bookkeeping()

    content_len = int(self.headers.getheader('content-length', 0))
    post_body = self.rfile.read(content_len)
    for pair in post_body.split(';'):
      x, y = map(int, pair.split(','))
      device.touch(x, y, MonkeyDevice.DOWN_AND_UP)
      time.sleep(0.08)
    
class StopThread(Thread):
  def __init__(self):
    Thread.__init__(self)

  def run(self):
    while True:
      time_since = current_time_ms() - last_used
      if time_since > 90000:
        print 'timed out, EXITING!'
        time.sleep(1)
        os._exit()
      
      time.sleep(1)

if __name__ == '__main__':
  StopThread().start()

  print 'Waiting for device connection...'
  device = MonkeyRunner.waitForConnection()

  server = HTTPServer(('localhost', 8080), PostHandler)
  print 'Got device, starting server, use <Ctrl-C> to stop'
  server.serve_forever()
