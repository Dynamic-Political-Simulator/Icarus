from pandas import DataFrame, Series
import matplotlib.pyplot as plt
from numpy.random import randn
from flask import Flask, jsonify, request
import os
import sys
import base64
import json



app = Flask(__name__)

@app.route("/genChart/", methods = ["POST"])
def GenChart():
    r = request.get_json()
    data : ChartInfoDto = ChartInfoDto(r)
    df = data.values
    print("here")
    #df = DataFrame([float(i) for i in values.split(',')])
    df.plot()
    plt.axhline(y = float(data.goal), color = 'b', label = 'Goal')
    plt.legend().remove()
    plt.ylabel(ylabel=data.label)
    plt.xlabel(xlabel='Ticks')
    #os.chdir("..")
    #os.chdir("./Icarus/Images/")
    #plt.savefig(f"{os.curdir}chart")
    import io
    my_stringIObytes = io.BytesIO()
    plt.savefig(my_stringIObytes, format='jpg')
    my_stringIObytes.seek(0)
    my_base64_jpgData = base64.b64encode(my_stringIObytes.read()).decode()
    m = {
        "Base64String" : my_base64_jpgData
    }
    
    return m


@app.route("/ping/")
def Pong():
    return "Pong"        

#string = "28.5,28.5,28.5,28.5,28.5,28.5,33.45,37.9,41.91,45.52,48.77,51.69,54.32,56.69,58.82,60.74,62.47,64.02,65.42,66.68,67.81,68.83,69.75,70.58,71.32,71.99,72.59,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73"
#df = DataFrame([float(i) for i in sys.argv[1].split(',')])
#vals = list(string)
#GenChart(df,float(sys.argv[2]))
#GenChart(df,10)

class ChartInfoDto:
    values : DataFrame
    goal : float
    label : str

    def __init__(self,j):
        #dic = json.loads(j)
        self.goal = j['goal']
        l : list = j['values']
        self.values = DataFrame(l)
        self.label = j['label']

app.run()