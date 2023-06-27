from pandas import DataFrame, Series
import matplotlib.pyplot as plt
from numpy.random import randn
from flask import Flask
import os
import sys

app = Flask(__name__)

@app.route("/genChart/<values>/<valueGoal>/")
def GenChart(values, valueGoal):
    df = DataFrame([float(i) for i in values.split(',')])
    df.plot()
    plt.axhline(y = valueGoal, color = 'b', label = 'Goal')
    plt.legend().remove()
    os.chdir("..")
    os.chdir("./Icarus/Images/")
    plt.savefig(f"{os.curdir}chart")
    return f"Saved Chart at {os.curdir}chart!"

@app.route("/ping/")
def Pong():
    return "Pong"        

#string = "28.5,28.5,28.5,28.5,28.5,28.5,33.45,37.9,41.91,45.52,48.77,51.69,54.32,56.69,58.82,60.74,62.47,64.02,65.42,66.68,67.81,68.83,69.75,70.58,71.32,71.99,72.59,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73,73"
#df = DataFrame([float(i) for i in sys.argv[1].split(',')])
#vals = list(string)
#GenChart(df,float(sys.argv[2]))
#GenChart(df,10)


app.run(port= os.getenv("PORT",5000))