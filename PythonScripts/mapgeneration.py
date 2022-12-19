import json
from dataclasses import dataclass
import cv2 as cv
import numpy as np
import base64

@dataclass
class MapObject:
    #id:int
    sprite:np.ndarray
    pos_x:float
    pos_y:float

map_height = 2000
map_width = 2000

def main(raw_data):
    data = json.loads(raw_data)
    Map_Objects:list = []
    for entry in data['map_objects']:
        object = MapObject(
            sprite=entry['sprite'],
            pos_x=entry['pos_x'],
            pos_y=entry['pos_y']
        )
        png_original = base64.b64decode(entry['sprite'])
        png_as_np = np.frombuffer(png_original, dtype=np.uint8)
        object.sprite = cv.imdecode(png_as_np, cv.IMREAD_UNCHANGED)
        Map_Objects.append(object)        
    map = np.zeros((map_height,map_width,4),np.uint8)
    map[:] = (255,255,255,255) 
    map_object:MapObject
    for map_object in Map_Objects:
        y1, y2 = map_object.pos_y, map_object.pos_y + map_object.sprite.shape[0]
        x1, x2 = map_object.pos_x, map_object.pos_x + map_object.sprite.shape[1]

        alpha_s = map_object.sprite[:, :, 3] / 255.0
        alpha_l = 1.0 - alpha_s

        for c in range(0, 3):
            map[y1:y2, x1:x2, c] = (alpha_s * map_object.sprite[:, :, c] +
                                    alpha_l * map[y1:y2, x1:x2, c])
    
    return base64.b64encode(cv.imencode(ext='.png',img=map)[1])

def Test():
    graphic = np.zeros((100,100,4),np.uint8)
    graphic[:,:,3]=255
    graphic = cv.imencode(ext='.png', img=graphic)[1]
    sprite = base64.b64encode(graphic).decode('ascii')
    data = {
        'map_objects':[
            {
                'sprite': sprite,
                'pos_x': 100,
                'pos_y': 100,
            },
            {
                'sprite': sprite,
                'pos_x': 450,
                'pos_y': 540
            }
        ]
    }
    map = main(json.dumps(data))
    png_original = base64.b64decode(map)
    png_as_np = np.frombuffer(png_original, dtype=np.uint8)
    img = cv.imdecode(png_as_np, flags=1)
    cv.imwrite('sample_image.png',img=img)

Test()

