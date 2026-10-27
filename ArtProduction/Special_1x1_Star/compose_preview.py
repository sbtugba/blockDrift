from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
p=Path(__file__).parent
canvas=Image.new('RGB',(1600,1040),'#f1ede7')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',25)
for filename,label,box in [('Perspective40.png','Perspective 40 deg',(0,50,1000,1000)),('Top.png','Top',(1010,50,1590,520)),('Side.png','Side',(1010,555,1590,1025))]:
    im=Image.open(p/filename).convert('RGB');im.thumbnail((box[2]-box[0],box[3]-box[1]))
    canvas.paste(im,(box[0]+(box[2]-box[0]-im.width)//2,box[1]+(box[3]-box[1]-im.height)//2))
    ImageDraw.Draw(canvas).text((box[0]+18,box[1]-35),label,fill='#3a342b',font=font)
canvas.save(p/'Star_ThreeViews.png')
