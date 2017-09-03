# TextureRecombination
 ## 目的：
 离线工具代码，将经过预处理的非pot尺寸的图片，打碎重组成pot尺寸，并在游戏运行时还原。

 ## 需求
 游戏中UI用的很多图片资源都是非pot尺寸，比如700x400。但是对于一些纹理压缩算法来说，pot尺寸才是理想的，比如512x512这样。

 ## 步骤
 1. 将一些非pot的图片，经过一定预处理(缩放)，转换成可以塞进pot尺寸的大小，比如700x400 => 640x384 < 512x512。（这一步其实也可以用代码处理，现阶段先用PS搞一下吧）。
 2. 离线运行工具代码，打碎并重新生成图片资源及对应的UV信息
 3. 运行游戏，根据对应新图片的UV信息重组图片。


 原图素材：640x358
[原图来源](https://cdn.pixabay.com/photo/2016/01/29/01/04/billiards-1167221_960_720.jpg)



[在Unity中的效果](http://wx1.sinaimg.cn/mw690/6b98bc8agy1fj6k7jl3d2j21hc0fyaw1.jpg)



预处理：630x378

[预处理](http://wx1.sinaimg.cn/mw690/6b98bc8agy1fj6k7pwohqj21gv0fink0.jpg)



打碎重组的效果图(512x512)：

[打碎重组](http://wx3.sinaimg.cn/mw690/6b98bc8agy1fj6k7ddulbj21gv0iyx1q.jpg)

