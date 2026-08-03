# render_community_logos.py — logos COMUNIDADE (sem fins lucrativos, ajuda mutua)
# 6 variacoes cobrindo as 4 direcoes + board de elementos + board de aplicacoes web/mobile
# Render 2x + downscale LANCZOS. Saida: docs/visual-explorations/cNN-*.png (+ system e applications)
import math, os
from PIL import Image, ImageDraw, ImageFont

FONTS = r"C:\Users\rodne\.kilo\skills\canvas-design\canvas-fonts"
OUT = os.path.dirname(os.path.abspath(__file__))
W, H, SCALE = 1200, 1500, 2
INK=(10,10,10); CHARCOAL=(23,23,23); SMOKE=(38,38,38); SILVER=(150,150,150); WHITE=(250,250,250)
EMERALD=(16,185,129); SKY=(14,165,233); PURPLE=(168,85,247); PINK=(236,72,153)
AMBER=(245,158,11); TERRACOTA=(188,108,37); CREAM=(254,250,224); LIME=(132,204,22); GOLD=(234,179,8)
DEEP_GREEN=(6,78,59)

def font(name,size): return ImageFont.truetype(os.path.join(FONTS,name),int(size*SCALE))
def canvas(bg=INK):
    img=Image.new("RGB",(W*SCALE,H*SCALE),bg); return img,ImageDraw.Draw(img)
def save(img,path):
    img.resize((W,H),Image.LANCZOS).save(path,"PNG"); print("gerado:",os.path.basename(path))
def lerp(a,b,t): return tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))
def ctext(d,text,fnt,cx,cy,fill):
    b=d.textbbox((0,0),text,font=fnt); w=b[2]-b[0]; h=b[3]-b[1]
    d.text((cx-w/2-b[0],cy-h/2-b[1]),text,font=fnt,fill=fill)
def rtext(d,text,fnt,x,y,fill): d.text((x,y),text,font=fnt,fill=fill)
def label(d,text,x,y,fnt=None,fill=SILVER):
    fnt=fnt or font("WorkSans-Regular.ttf",13); rtext(d,text.upper(),fnt,x,y,fill)
def wordmark(d,cx,cy,fill=WHITE,sz=92,serif=False):
    f=font("YoungSerif-Regular.ttf" if serif else "WorkSans-Bold.ttf",sz); ctext(d,"revoa.me",f,cx,cy,fill)
def rmoney(d,cx,cy,fill=AMBER,sz=36):
    ctext(d,"RM$",font("WorkSans-Bold.ttf",sz),cx,cy,fill)
def slogan(d,text,cx,cy,fill=SILVER,sz=20,serif=True):
    ctext(d,text,font("YoungSerif-Regular.ttf" if serif else "WorkSans-Regular.ttf",sz),cx,cy,fill)

# ─── C1 · Maos em Circulo (coletivo/pessoa-a-pessoa) ─────────────
def c01_maos_circulo():
    img,d=canvas(INK); cx,cy=600*SCALE,640*SCALE
    # 6 "pessoas" (cabeca + corpo) em circulo = comunidade de maos dadas
    import math as m
    R=240*SCALE
    cols=[EMERALD,AMBER,SKY,TERRACOTA,PINK,LIME]
    for i in range(6):
        a=m.radians(-90+i*60); px=cx+m.cos(a)*R; py=cy+m.sin(a)*R
        d.ellipse([px-34*SCALE,py-70*SCALE,px+34*SCALE,py-2*SCALE],fill=cols[i])      # corpo
        d.ellipse([px-22*SCALE,py-120*SCALE,px+22*SCALE,py-76*SCALE],fill=cols[i])     # cabeca
    # circulo central = "revoa" (ajuda mutua conectando)
    d.ellipse([cx-90*SCALE,cy-90*SCALE,cx+90*SCALE,cy+90*SCALE],fill=CHARCOAL,outline=EMERALD,width=int(4*SCALE))
    wordmark(d,cx,640*SCALE)
    slogan(d,"Comunidade que troca, doa e cuida",cx,750*SCALE,fill=AMBER,sz=22)
    rmoney(d,cx,840*SCALE,fill=SILVER,sz=30)
    label(d,"C1 · Maos em Circulo",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),EMERALD)
    label(d,"comunidade · coletivo / pessoa-a-pessoa",80*SCALE,112*SCALE,fill=SILVER)
    save(img,os.path.join(OUT,"c01-maos-circulo.png"))

# ─── C2 · Revoar / Segunda Vida (reaproveitar) ───────────────────
def c02_segunda_vida():
    img,d=canvas(INK); cx,cy=600*SCALE,640*SCALE
    # duas setas circulares = ciclo/segunda vida
    for k,(r,col) in enumerate([(260,EMERALD),(200,SKY)]):
        d.arc([cx-r,cy-r,cx+r,cy+r],40,200,fill=col,width=int(16*SCALE))
    # "ponto" pousando em novo lar (seta de chegada)
    d.polygon([(cx+260*SCALE,cy),(cx+330*SCALE,cy-30*SCALE),(cx+330*SCALE,cy+30*SCALE)],fill=AMBER)
    d.ellipse([cx-46*SCALE,cy-46*SCALE,cx+46*SCALE,cy+46*SCALE],fill=AMBER)
    wordmark(d,cx,660*SCALE)
    slogan(d,"Tudo encontra um novo lar",cx,770*SCALE,fill=AMBER,sz=22)
    rmoney(d,cx,860*SCALE,fill=SILVER,sz=30)
    label(d,"C2 · Segunda Vida",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),EMERALD)
    label(d,"comunidade · reaproveitar / ciclo",80*SCALE,112*SCALE,fill=SILVER)
    save(img,os.path.join(OUT,"c02-segunda-vida.png"))

# ─── C3 · Passaro-Folha (natureza + reaproveitar) ────────────────
def c03_passaro_folha():
    img,d=canvas(DEEP_GREEN); cx,cy=600*SCALE,640*SCALE
    # corpo do passaro = folha (curva verde-lima)
    d.pieslice([cx-180*SCALE,cy-90*SCALE,cx+180*SCALE,cy+120*SCALE],200,340,fill=LIME)
    # asa
    d.polygon([(cx-40*SCALE,cy-10*SCALE),(cx-180*SCALE,cy-90*SCALE),(cx-20*SCALE,cy-60*SCALE)],fill=EMERALD)
    # bico + olho
    d.polygon([(cx+150*SCALE,cy),(cx+210*SCALE,cy-20*SCALE),(cx+150*SCALE,cy+20*SCALE)],fill=GOLD)
    d.ellipse([cx+110*SCALE,cy-26*SCALE,cx+132*SCALE,cy-4*SCALE],fill=INK)
    # ramo
    d.line([(cx-180*SCALE,cy+120*SCALE),(cx-120*SCALE,cy+120*SCALE)],fill=GOLD,width=int(5*SCALE))
    wordmark(d,cx,680*SCALE,fill=WHITE)
    slogan(d,"Da nova vida ao que voce tem",cx,790*SCALE,fill=LIME,sz=22)
    rmoney(d,cx,880*SCALE,fill=GOLD,sz=30)
    label(d,"C3 · Passaro-Folha",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),LIME)
    label(d,"comunidade · natureza / sustentabilidade",80*SCALE,112*SCALE,fill=CREAM)
    save(img,os.path.join(OUT,"c03-passaro-folha.png"))

# ─── C4 · Coracao Comunitario (organico / mao-amiga / calido) ─────
def c04_coracao_comunitario():
    img,d=canvas(INK); cx,cy=600*SCALE,640*SCALE
    # coracao geometrico de duas maos/formas arredondadas
    d.pieslice([cx-150*SCALE,cy-150*SCALE,cx+10*SCALE,cy+10*SCALE],180,360,fill=PINK)
    d.pieslice([cx-10*SCALE,cy-150*SCALE,cx+150*SCALE,cy+10*SCALE],180,360,fill=EMERALD)
    d.polygon([(cx-150*SCALE,cy-50*SCALE),(cx+150*SCALE,cy-50*SCALE),(cx,cy+170*SCALE)],fill=lerp(PINK,EMERALD,0.5))
    wordmark(d,cx,680*SCALE)
    slogan(d,"Onde a ajuda tem asas",cx,790*SCALE,fill=PINK,sz=22)
    rmoney(d,cx,880*SCALE,fill=SILVER,sz=30)
    label(d,"C4 · Coracao Comunitario",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),PINK)
    label(d,"comunidade · organico / mao-amiga",80*SCALE,112*SCALE,fill=SILVER)
    save(img,os.path.join(OUT,"c04-coracao-comunitario.png"))

# ─── C5 · Broto / Nova Vida (natureza / sustentabilidade) ────────
def c05_broto():
    img,d=canvas(DEEP_GREEN); cx,cy=600*SCALE,640*SCALE
    # caule
    d.line([(cx,cy+150*SCALE),(cx,cy-80*SCALE)],fill=LIME,width=int(12*SCALE))
    # duas folhas (broto crescendo)
    d.pieslice([cx-120*SCALE,cy-120*SCALE,cx+20*SCALE,cy+20*SCALE],40,220,fill=EMERALD)
    d.pieslice([cx-20*SCALE,cy-160*SCALE,cx+120*SCALE,cy-20*SCALE],320,140,fill=LIME)
    # "semente" base = item que ganha nova vida
    d.ellipse([cx-50*SCALE,cy+120*SCALE,cx+50*SCALE,cy+220*SCALE],fill=GOLD)
    wordmark(d,cx,720*SCALE,fill=WHITE)
    slogan(d,"Mais comunidade, menos desperdicio",cx,830*SCALE,fill=LIME,sz=20)
    rmoney(d,cx,920*SCALE,fill=GOLD,sz=30)
    label(d,"C5 · Broto / Nova Vida",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),LIME)
    label(d,"comunidade · natureza / sustentabilidade",80*SCALE,112*SCALE,fill=CREAM)
    save(img,os.path.join(OUT,"c05-broto.png"))

# ─── C6 · Casa-Ninho (comunidade / hiperlocal / novo lar) ─────────
def c06_casa_ninho():
    img,d=canvas(INK); cx,cy=600*SCALE,640*SCALE
    # telhado
    d.polygon([(cx,cy-180*SCALE),(cx-190*SCALE,cy-10*SCALE),(cx+190*SCALE,cy-10*SCALE)],fill=TERRACOTA)
    # corpo da casa
    d.rectangle([cx-150*SCALE,cy-10*SCALE,cx+150*SCALE,cy+170*SCALE],fill=lerp(EMERALD,CHARCOAL,0.4),outline=EMERALD,width=int(4*SCALE))
    # porta = "ninho / novo lar"
    d.rectangle([cx-46*SCALE,cy+40*SCALE,cx+46*SCALE,cy+170*SCALE],fill=AMBER)
    d.arc([cx-46*SCALE,cy+30*SCALE,cx+46*SCALE,cy+120*SCALE],180,360,fill=GOLD,width=int(5*SCALE))
    wordmark(d,cx,720*SCALE)
    slogan(d,"Ajuda mutua, de vizinho para vizinho",cx,830*SCALE,fill=TERRACOTA,sz=18)
    rmoney(d,cx,920*SCALE,fill=SILVER,sz=30)
    label(d,"C6 · Casa-Ninho",80*SCALE,80*SCALE,font("WorkSans-Bold.ttf",16),TERRACOTA)
    label(d,"comunidade · hiperlocal / novo lar",80*SCALE,112*SCALE,fill=SILVER)
    save(img,os.path.join(OUT,"c06-casa-ninho.png"))

# ─── SYSTEM · Elementos separados (logo/icon/favicon/slogan) ──────
def system_board():
    img,d=canvas(WHITE); cx=W*SCALE/2
    label(d,"ELEMENTOS DE MARCA — separados",80*SCALE,60*SCALE,font("WorkSans-Bold.ttf",18),INK)
    # 1. Wordmark (logo) isolado
    wordmark(d,cx,200*SCALE,fill=INK,sz=80)
    label(d,"LOGO / wordmark",cx-180*SCALE,150*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # 2. Icone (monograma RV) em 3 tamanhos
    for i,(sz,yy) in enumerate([(260,440),(160,460),(90,475)]):
        ix=200*SCALE + i*360*SCALE
        d.ellipse([ix-sz*SCALE,yy*SCALE-sz*SCALE/2,ix+sz*SCALE,yy*SCALE+sz*SCALE/2],fill=INK)
        ctext(d,"RV",font("WorkSans-Bold.ttf",sz*0.5),ix,yy*SCALE,EMERALD)
    label(d,"ICONE / monograma RV (256 / 152 / 96)",80*SCALE,360*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # 3. Favicons (32 e 64) sobre fundos
    for i,(sz,bg) in enumerate([(64,INK),(64,EMERALD),(32,INK)]):
        fx=200*SCALE+i*200*SCALE; fy=700*SCALE
        d.rectangle([fx,fx if False else fy,fx+sz*SCALE,fy+sz*SCALE],fill=bg,outline=SILVER,width=int(2*SCALE))
        ctext(d,"RV",font("WorkSans-Bold.ttf",sz*0.45),fx+sz*SCALE/2,fy+sz*SCALE/2,EMERALD if bg==INK else WHITE)
    label(d,"FAVICON / app icon (64 ink, 64 emerald, 32 ink)",80*SCALE,640*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # 4. Simbolo monetario
    rmoney(d,300*SCALE,900*SCALE,fill=EMERALD,sz=70)
    label(d,"SIMBOLO RM$ (moeda social)",80*SCALE,870*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # 5. Opcoes de slogan
    label(d,"OPCOES DE SLOGAN (escolher uma)",640*SCALE,860*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    slogans=["1. Tudo encontra um novo lar.","2. Da nova vida ao que voce tem.",
             "3. Comunidade que troca, doa e cuida.","4. Ofereca o que sabe. Receba o que precisa.",
             "5. Mais comunidade, menos desperdicio.","6. Onde a ajuda tem asas.",
             "7. Ajuda mutua, de vizinho para vizinho.","8. O que voce nao usa encontra quem precisa."]
    for i,s in enumerate(slogans):
        rtext(d,s,font("YoungSerif-Regular.ttf",24),640*SCALE,(910+i*48)*SCALE,fill=INK)
    save(img,os.path.join(OUT,"c00-system-elementos.png"))

# ─── APPLICATIONS · web + mobile ─────────────────────────────────
def applications_board():
    img,d=canvas(INK); cx=W*SCALE/2
    label(d,"APLICACOES — web e mobile",80*SCALE,60*SCALE,font("WorkSans-Bold.ttf",18),EMERALD)
    # ---- WEB: header mock ----
    hx,hy,hw,hh=80*SCALE,150*SCALE,560*SCALE,90*SCALE
    d.rounded_rectangle([hx,hy,hx+hw,hy+hh],radius=int(12*SCALE),fill=CHARCOAL,outline=SMOKE,width=int(2*SCALE))
    # logo no header
    wordmark(d,hx+150*SCALE,hy+hh/2,fill=WHITE,sz=26)
    # nav
    for i,t in enumerate(["Feed","Comunidade","Doar","Carteira"]):
        rtext(d,t,font("WorkSans-Regular.ttf",16),hx+320*SCALE+i*120*SCALE,hy+hh/2-12*SCALE,fill=SILVER)
    # saldo RM$ no header
    d.rounded_rectangle([hx+hw-150*SCALE,hy+18*SCALE,hx+hw-10*SCALE,hy+hh-18*SCALE],radius=int(8*SCALE),fill=SMOKE)
    ctext(d,"RM$ 120",font("WorkSans-Bold.ttf",18),hx+hw-80*SCALE,hy+hh/2,fill=AMBER)
    label(d,"WEB — header (logo + nav + saldo)",80*SCALE,118*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # ---- WEB: card de anuncio (doacao) ----
    bx,by,bw,bh=80*SCALE,290*SCALE,260*SCALE,200*SCALE
    d.rounded_rectangle([bx,by,bx+bw,by+bh],radius=int(14*SCALE),fill=CHARCOAL,outline=SMOKE,width=int(2*SCALE))
    d.rounded_rectangle([bx+14*SCALE,by+14*SCALE,bx+bw-14*SCALE,by+120*SCALE],radius=int(10*SCALE),fill=DEEP_GREEN)
    d.rounded_rectangle([bx+14*SCALE,by+130*SCALE,bx+90*SCALE,by+160*SCALE],radius=int(6*SCALE),fill=PINK)
    rtext(d,"DOAR",font("WorkSans-Bold.ttf",14),bx+24*SCALE,by+134*SCALE,fill=WHITE)
    rtext(d,"Cadeira boa, novo lar",font("WorkSans-Bold.ttf",18),bx+14*SCALE,by+172*SCALE,fill=WHITE)
    rtext(d,"Gratis · RM$ 0",font("WorkSans-Regular.ttf",14),bx+14*SCALE,by+150*SCALE,fill=SILVER)
    label(d,"WEB — card de anuncio (badge Doar)",80*SCALE,258*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # ---- MOBILE: app icon ----
    mx,my,mw=720*SCALE,160*SCALE,260*SCALE
    d.rounded_rectangle([mx,my,mx+mw,my+mw],radius=int(56*SCALE),fill=EMERALD)
    ctext(d,"RV",font("WorkSans-Bold.ttf",120),mx+mw/2,my+mw/2,fill=WHITE)
    label(d,"MOBILE — app icon (PWA)",720*SCALE,128*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # ---- MOBILE: splash ----
    sx,sy,sw,sh=720*SCALE,470*SCALE,260*SCALE,520*SCALE
    d.rounded_rectangle([sx,sy,sx+sw,sy+sh],radius=int(28*SCALE),fill=CHARCOAL,outline=SMOKE,width=int(3*SCALE))
    d.ellipse([sx+sw/2-60*SCALE,sy+80*SCALE,sx+sw/2+60*SCALE,sy+200*SCALE],fill=EMERALD)
    ctext(d,"RV",font("WorkSans-Bold.ttf",60),sx+sw/2,sy+140*SCALE,fill=WHITE)
    wordmark(d,sx+sw/2,sy+260*SCALE,fill=WHITE,sz=30)
    slogan(d,"Tudo encontra um novo lar",sx+sw/2,sy+320*SCALE,fill=AMBER,sz=12)
    # feed mini no splash
    for i in range(3):
        d.rounded_rectangle([sx+20*SCALE,sy+360*SCALE+i*46*SCALE,sx+sw-20*SCALE,sy+396*SCALE+i*46*SCALE],radius=int(8*SCALE),fill=SMOKE)
    label(d,"MOBILE — splash + home",720*SCALE,438*SCALE,font("WorkSans-Regular.ttf",12),SILVER)
    # ---- footer nota ----
    rtext(d,"revoa.me (app) · revoa.org (transparencia)",font("WorkSans-Regular.ttf",16),80*SCALE,1080*SCALE,fill=SILVER)
    save(img,os.path.join(OUT,"c00-applications-web-mobile.png"))

if __name__=="__main__":
    c01_maos_circulo(); c02_segunda_vida(); c03_passaro_folha()
    c04_coracao_comunitario(); c05_broto(); c06_casa_ninho()
    system_board(); applications_board()
    print("concluido.")
