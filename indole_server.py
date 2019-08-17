import uuid
import subprocess
from flask import Flask
import json
import threading

app = Flask(__name__)


aeskey = str(uuid.uuid1()).replace('-', '')
port = 12345
remoteHost = '45.32.129.138:%d' % port


def refresh_config():
    global aeskey
    global port
    global remoteHost
    
    aeskey = str(uuid.uuid1()).replace('-', '')
    port = port + 1
    remoteHost = '45.32.129.138:%d' % port

    config = '<indole>\n<tcpaes network="tcp" address="0.0.0.0:%d" bufsize="4096">\n<encode>\n<aesdec queue_size="1024" hex_key="%s" buf_size="8192"/>\n</encode>\n<decode>\n<aesenc queue_size="1024" hex_key="%s"/>\n</decode>\n<tcp network="tcp" address="localhost:8118"/>\n</tcpaes>\n</indole>' % (port, aeskey, aeskey)
    
    print(config)

    f = open('config.xml', 'w')
    f.write(config)
    f.close()


proc = None
def start_indole():
    global proc
    if proc != None:
        try:
            proc.kill()
        except Exception as error:
            print(error)
        proc = None
    proc = subprocess.Popen('./indole < config.xml', cwd = '/root/golang/indole', shell=True)



def timerRefreshConfig():  
    print("refresh config...." )
 
    refresh_config()
    start_indole()
    
    timer = threading.Timer(60 * 60 * 24,timerRefreshConfig)
    timer.start()

timerRefreshConfig()


@app.route("/indoleVPN/api/config")
def get_config():
    global aeskey
    global remoteHost
    return json.dumps({ 'aesKey': aeskey, 'remoteHost': remoteHost })


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5003)

 


