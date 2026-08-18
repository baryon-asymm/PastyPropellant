c ######################################################
c  Пpогpамма моделирования стpуктуpы СТРТ
c ######################################################
	USE DFPORT
      REAL*8 KCIL,MD4,MD3, DD4, DD3, VVKS
	REAL Vmkm_loc, Vdok_loc, VSMKM1, SVD1, mkmcoef, karmcoef
	Real,allocatable::Vmkm_total(:),Vdok_total(:),VSMKM(:),SVD(:)
	Real,allocatable::gk0(:), gk1(:)
	INTEGER*8 :: NFX=0, NFY=0, NFZ=0, NFQ=0, NFW=0, QKSS
      REAL,allocatable :: VKSO(:),QKS1(:),FQKS(:),DPOC(:)
	real,allocatable ::  fmkarm2(:), fmkarm2_loc(:), fmkarm_cor(:)
	integer*8, allocatable :: fqkarm_cor(:)
	real*8,allocatable :: VKS(:) 

	integer,allocatable :: SFR(:)
	integer*8,allocatable :: QKS(:), qmkm1(:), qmkm2(:), coef(:)
	integer qmkm1_nmax, qmkm2_nmax, coef_nmax
      REAL,allocatable :: GDOK(:),DDOK(:)
	REAL*8,allocatable :: ZX(:),Z11(:)

	integer DPRow, DDrow
	REAL,allocatable :: DOKP31(:),DOKP41(:),DOKP43(:),DOKP431(:)
	REAL,allocatable :: DOKP432(:), qdokkarm(:)
      REAL*8,allocatable :: MDOK3(:), MDOK4(:), DDOK3(:), DDOK4(:)
	REAL,allocatable :: Epsydok(:), Epsmdok3(:), Epsmdok4(:) 
	REAL*8,allocatable :: VDOKS(:,:), VVDOKS(:)
	REAL,allocatable :: VDOKSO(:,:), QDOKS1(:,:), Dpockets(:)
	real, allocatable :: QDOKSO(:,:)
	integer*8,allocatable :: QDOKS(:,:), QDOKSS(:), QDOK(:)

	integer Xseed(1), shift_flag, attempt
	real nn, nn_total, nn_min, nn_max, mp, eta
	real jammed_total

	real,allocatable :: ALLVDOK(:), ALLVDOKSO(:), FMDOK(:)
	real,allocatable :: epsalldok(:), ALLVDOK_FR(:)	
	real,allocatable :: zdoksmall(:),vdoksmall(:),Vdokstr(:)
	real,allocatable :: ddoksmall(:),pdoksmall(:)
  	integer*8,allocatable :: ALLDOK_fract(:), ALLDOK(:),qdokstr(:)
	integer*8 ALLDOKQ
	
	real*8 DOK_base41,DOK_base31,DOK_sur41, DOK_sur31      
      real*8 X,X0,X1,X2,X21,X3,x4,alfa
      real*8 xss0,xss1,xss2,xss3,xss4,xss5,xss6
      real*8 d31,d41,sd3,sd4,dp41,dp31
	integer*8 conditions(10)
      integer hour,minut,sec,hour1,minut1,sec1,fi,gsv,ii

	real,allocatable :: par1(:),par2(:),par3(:),par4(:),par5(:),par6(:)
 
      REAL AX(6),BX(6),xx(20)
      integer*4 u06/1/,u16/0/,u26/0/,u36/0/,u46/0/,
     &u56/0/,u66/0/,u76/0/,u86/0/,u96/0/         
      integer*4 u05/1/,u15/0/,u25/7916/,u35/6769/,u45/8113/,
     &u55/7234/,u65/4142/,u75/5015/,u85/3567/,u95/1526/                                  
      integer*4 u04/1/,u14/0/,u24/7640/,u34/5347/,u44/2291/,
     &u54/4799/,u64/945/,u74/6715/,u84/5714/,u94/914/                                               
      integer*4 u03/1/,u13/0/,u23/7364/,u33/3925/,u43/7109/,
     &u53/884/,u63/2888/,u73/4164/,u83/3784/,u93/1899/
      integer*4 u02/1/,u12/0/,u22/7088/,u32/2503/,u42/6183/,
     &u52/3683/,u62/6066/,u72/4620/,u82/7519/,u92/1298/
      integer*4 u01/1/,u11/0/,u21/6812/,u31/1081/,u41/7705/,
     &u51/5003/,u61/6576/,u71/7149/,u81/6654/,u91/1302/
      CHARACTER FLDN*8,fname*8, answer, answer1, name*32
        
	Di=10.e-6
	Dj=10.e-6
      Dmin=10.e-6
	alpha = 0.25
	nn_min = 3.
	nn_max = 1.e2
	eps_dok = 5.e-2
	alfa = 0.
	gdokns = 0.
	eta = 0.
	fname = 'results'
	answer1 = '0'
	ivar = 0
	mkmcoef = 7.73
	karmcoef = 8.2
c       #####################################################
	print*,'   *********************************************************'
	print*,'   **                                                     **'
	print*,'   ** Solid composite propellant microstructure model     **'
	print*,'   **                                                     **'
	print*,'   **                                                     **'
	print*,'   *********************************************************'
	write(*,*)
 1002 print'(1x,a/,a\)','Enter name of input file (max 8 signs)','>> '
      READ (*,1001) FLDN
	write(*,*)
 1001 FORMAT(A8)
      OPEN(2,FILE=trim(FLDN)//'.DAT',STATUS='OLD',err=9182)
      READ(2,1000)
 1000 FORMAT(2X)
      goto 9183
 9182 write(*,'(1x,a)')"ERROR! File doesn't exist."
	write(*,*)
      goto 1002
 9183 open(4,file=trim(fname)//'.m',status='new',err=9184)
      go to 9185
 9184 print'(1x,a)','Output file already exists, rewrite? [Y/N]'
	print'(a\)','>> '
 9186 read(*,*)answer
	write(*,*)
      if(answer.eq.'n')then
	  print'(1x,a/,a\)','Enter name of output file (max 8 signs)','>> '
        read(*,*)fname
	  write(*,*)
        open(4,file=trim(fname)//'.m',status='new',err=9184)
        goto 9185
      end if
	if(answer.eq.'y')then
          open(4,file=trim(fname)//'.m',status='old')
          close(4,status='delete')
          open(4,file=trim(fname)//'.m',status='new')
          goto 9185
      end if
      go to 9186
c
c Reading from file
c       #####################################################
9185  CONTINUE
      READ(2,*)PLOT1,PLOT2,GGG0,GM
      READ(2,1000)
      READ(2,*)AK1,AK2,AK3,AK4 
      READ(2,1000)
      READ(2,*)NMM,JZZ,KXX,N,NNZ,GSV
      READ(2,1000)
	Allocate(GDOK(NMM),DDOK(2*NMM),SFR(NMM))
      READ(2,*)(GDOK(II),II=1,NMM)
      READ(2,1000)
      READ(2,*)(DDOK(II),II=1,2*NMM)
 119	Continue
	SFR = 1
	if (KXX.GT.1) then
c		open(3,file='result.tab',status='old',err=1111)
c		close(3,status='delete')
c 1111		open(3,file='result.tab',status='new')
	allocate(par1(KXX),par2(KXX),par3(KXX),par4(KXX),par5(KXX),par6(KXX))
	else
		KXX = 1
	endif
c       #####################################################
	print'(1x,a)',"Do you want to use default parameters? [Y/N]"
 	print'(a\)','>> '
	read(*,*)answer
	write(*,*)
	if(answer.eq.'n')then
 	write(*,*)'Select parameter or continue:'
	write(*,*)
	write(*,*)            ' [0]: Start modeling'
	write(*,'(1x,a,F6.2)')' [1]: Dok_min = ', Dmin*1e6
	write(*,'(1x,a,F6.2)')' [2]: Di      = ', Di*1e6
	write(*,'(1x,a,F6.2)')' [3]: Dj      = ', Dj*1e6
	write(*,'(1x,a,F5.2)')' [4]: eps     = ', eps_dok
	write(*,'(1x,a,F5.2)')' [5]: k5      = ', alpha
	write(*,'(1x,a,F5.2)')' [6]: k6      = ', nn_min
	write(*,'(1x,a,F5.2)')' [7]: k7      = ', karmcoef
	write(*,'(1x,a,F5.2)')' [8]: k8      = ', mkmcoef
	write(*,'(1x,a,F5.2)')' [9]: P[Dok>Dok_max]  = ', real(alfa, kind = 4)
	write(*,'(1x,a,F7.2)')'[10]: max Nkarm/Nmkm = ', nn_max
	write(*,'(1x,a,F5.2)')'[11]: gdok homogenized = ', gdokns
	write(*,'(1x,a,a)')   '[12]: Read dok fr. from file  = ',trim(answer1)
	write(*,'(1x,a,I1)')  '[13]: Calculation variant = ', ivar
	write(*,'(1x,a,F5.2)')'[14]: eta = ', eta
141	write(*,*)	
	write(*,*)'Enter a number:'
	print'(a\)','>> '
 17	FORMAT(1x,a/,a\)
	read(*,*)nump
	write(*,*)
	if (nump.eq.0) then
		goto 142
	else if (nump.eq.1) then
	write(*,17)'Dok_min  - ? (Min. size of oxidizer, [mkm] or [m])','>> '
	read*, Dmin
	goto 141
	else if (nump.eq.2) then
	write(*,17)'Di  - ? (Histogram f(D) cell size, [mkm] or [m])','>> '
	read*, Di
	goto 141
	else if (nump.eq.3) then
	write(*,17)'Dj  - ? (Dkarm init. step in f(Dok|Dkarm), [mkm] or [m])',
     &'>> '
	read*, Dj
	goto 141
	else if (nump.eq.4) then
	write(*,17)'eps  - ? (Recommended = [0.01...0.05])','>> '
	read*, eps_dok
	goto 141
	else if (nump.eq.5) then
	write(*,17)'k5  - ? (Vdok coef.)','>> '
	read*, alpha
	goto 141
	else if (nump.eq.6) then
	write(*,17)'k6  - ? (min Nkarm/Nmkm)','>> '
	read*, nn_min
	goto 141
	else if (nump.eq.7) then
	write(*,17)'k7  - ? (P[karm-in-karm] coef.)','>> '
	read*, karmcoef
	goto 141
	else if (nump.eq.8) then
	write(*,17)'k8  - ? (P[karm-in-mkm] coef.)','>> '
	read*, mkmcoef
	goto 141
	else if (nump.eq.9) then
	write(*,17)'P[Dok>Dok_max] - ? (Recommended = 1e-5)','>> '
	read*, alfa
	goto 141
	else if (nump.eq.10) then
	write(*,17)'max Nkarm/Nmkm - ? (Recommended = [30...100])','>> '
	read*, nn_max
	goto 141
	else if (nump.eq.11) then
	write(*,17)'gdok - ? (Homogenized fraction of dok, [0...1])','>> '
	read*, gdokns
	goto 141
	else if (nump.eq.12) then
	write(*,17)"Read dok fract. that form pockets from file? [Y/N]",'>> '
	read*, answer1
	goto 141
	else if (nump.eq.13) then
	write(*,17)'Calculation var. - ? (0 = reset at Dbase < k1*Dok)','>> '
	read*, ivar
	goto 141
	else if (nump.eq.14) then
	write(*,17)"Fraction of aggl. oxide (eta) - ? [0...1]",'>> '
	read*, eta
	goto 141
	else if (nump.ge.15) then
c	write(*,17)"Continue - ? [Y/N]",'>> '
c	read*, answer
c		if (answer.eq.'y') then
c			goto 142
c		else
c			goto 141
c		endif
	endif

	end if

 142	continue
	if (answer1.eq.'y') then
			READ(2,1000)
 			READ(2,*) SFR
		endif
      CLOSE(UNIT=2)
	call gettim(ihr,imin,isec,i100th)
      hour=ihr
      minut=imin
      sec=isec
	write(*,'(/1X,a,I2,a,I2,a,I2,a\)')
     &'[',IHR,' : ',IMIN,' : ',isec, '] Modeling, '
	print'(a,I8,a,I3)','N =',N,', Cycles =',KXX
	write(*,*)
c ###################################
c	перевод из [мкм] в [м]	
	do kilo = 1, NMM
	if (DDOK(2*kilo).ge.0.1) DDOK(2*kilo) = DDOK(2*kilo)*1e-6
	if (DDOK(2*kilo-1).ge.0.1) DDOK(2*kilo-1) = DDOK(2*kilo-1)*1e-6
	enddo
	if (Di.ge.0.1) Di = Di*1e-6
	if (Dj.ge.0.1) Dj = Dj*1e-6
	if (Dmin.ge.0.1) Dmin = Dmin*1e-6
c	определение Ddokmax; Ndok, Nkarm, Ncat (число разрядов)
	DDokmax = 0.
	do kilo = 1, NMM
		if (DDOK(2*kilo).GT.DDokmax) then
			DDokmax = DDOK(2*kilo)
		endif
	enddo
	Ndok = int(Ddokmax/Di)+2
	Nkarm = int(Ddokmax*AK4/Di)+2
	Ncat = int(Ddokmax*AK4/Dj)+2
	Nc = 1000;
c	Allocating memory:
	Allocate(ZX(NMM),Z11(NMM+1))
	Allocate(ALLDOK_fract(NMM),epsalldok(NMM),FMDOK(Ndok+1))
	Allocate(ALLVDOK(Ndok),ALLVDOKSO(Ndok),ALLVDOK_FR(Ndok),ALLDOK(Ndok))
	Allocate(VSMKM(Ndok),SVD(Ndok),Vdok_total(Ndok),Vmkm_total(Ndok))	
	Allocate(VKSO(Nkarm),QKS1(Nkarm),FQKS(Nkarm+1),DPOC(Nkarm+1),gk0(Ndok))
	Allocate(QKS(Nkarm),fmkarm_cor(Nkarm),gk1(Ndok))
	Allocate(zdoksmall(Ndok),vdoksmall(Ndok),Vdokstr(Ndok))
	Allocate(ddoksmall(Ndok),pdoksmall(Ndok),qdokstr(Ndok))
	Allocate(VKS(Nkarm),fmkarm2(Nkarm),fmkarm2_loc(Nkarm))
	Allocate(fqkarm_cor(Nkarm), QDOKSO(Ncat,Ndok))
	Allocate(DOKP31(Ncat),DOKP41(Ncat),DOKP43(Ncat),DOKP431(Ncat))
	Allocate(DOKP432(Ncat),MDOK3(Ncat), MDOK4(Ncat), DDOK3(Ncat))
	Allocate(DDOK4(Ncat),Epsydok(Ncat), Epsmdok3(Ncat), Epsmdok4(Ncat)) 
	Allocate(VDOKS(Ncat,Ndok), VVDOKS(Ncat), Dpockets(Ncat))
	Allocate(VDOKSO(Ncat,Ndok), QDOKS1(Ncat,Ndok), qdokkarm(Ncat))
	Allocate(QDOKS(Ncat,Ndok), QDOKSS(Ncat), QDOK(Ncat))
	Allocate(qmkm1(Nc), qmkm2(Nc), coef(Nc))
c       #####################################################	
	IPRIS=0; KPRIS=0; ITS = 0
c анализ СВ
	XSS0=0.; XSS1=0.; XSS2=0.; XSS3=0.; XSS4=0.
c определение параметров базовых частиц
	Sd4=0.; Sd3=0.
c определение параметров частиц окружеиня 
	D41=0.; D31=0. 
c анализ Карманов
	DPmax = 0.; DPmax_cor = 0.
	VKSO = 0.;QKS1 = 0.; FQKS = 0.; DPOC = 0.; QKS = 0;	VKS = 0
	DP31 = 0.;	DP41 = 0.
	epsy = 0.; fmkarm2 = 0.; fmkarm_cor = 0.; fqkarm_cor = 0;
c анализ локальной структуры
	conditions = 0
	jammed_total = 0.
	ibridge_total = 0
	Vmkm_total = 0.
	Vdok_total = 0.
	Vmkm_total2 = 0.
	Vdok_total2 = 0.
	SvD=0.
	VSMKM=0.
	coef = 0; qmkm1=0; qmkm2=0
	qmkm1_nmax=0; qmkm2_nmax=0; coef_nmax=0
	gk0=0.; gk1=0.
c получение условных функций ДОК, формирующих Карманы
	DOKP41 = 0.
	DOKP31 = 0.
	DOKP43 = 0.
      DOKP431 = 0.
	VDOKS = 0.
	QDOK = 0
	QDOKS = 0
	QDOKSO = 0;
	qdokkarm = 0.;
c получение функции распределения всех ДОК
	ALLDOK_FRACT = 0
	ALLDOK = 0
	ALLVDOK = 0.
	ALLVDOK_FR = 0.
	DOK_base31 = 0
	DOK_base41 = 0
	DOK_sur31 = 0
	DOK_sur41 = 0
	vdokstr = 0.
	qdokstr = 0.
c       #####################################################
	if(gsv.eq.1)then
      A=0.12345678
      B=0.87654321
      AX(1)=A
      BX(1)=B
      KKZ=2     
 889  CONTINUE
      DO 887 JK=1,NNZ
		YZZ=RANDOM1(A,B)
          IF(JK.EQ.NNZ)THEN 
			AX(KKZ)=A
              BX(KKZ)=B
          END IF 
 887  CONTINUE 
      KKZ=KKZ+1
      IF(KKZ.EQ.7) GO TO 888
      GO TO 889
 888  CONTINUE
      A6=AX(1)
      B6=BX(1)
      A5=AX(2)
      B5=BX(2)
      A4=AX(3)
      B4=BX(3)
      A3=AX(4)
      B3=BX(4)
      A2=AX(5)
      B2=BX(5)
      A1=AX(6)
      B1=BX(6)
      end if
c       #####################################################
      GGG = GGG0 - gdokns*GGG0
	CALL PARAM(JZZ,NMM,GDOK,PLOT1,DDOK,alfa,ZX,Z11,ZSS,Dmax)
      sLamd=(GGG/PLOT1)*ZSS*PLOT2
      JK1=1
      DOKM=0.0
	DOKSD=0.0
      IF(JZZ.EQ.2) GO TO 901
      DO 801 KK1=1,2*NMM-1,2
          DOKM=DOKM+(DDOK(KK1)+DDOK(KK1+1))*GDOK(JK1)/2
		DOKSD = DOKSD + GDOK(JK1)*
     &           (DDOK(KK1)**2+DDOK(KK1)*DDOK(KK1+1)+DDOK(KK1+1)**2)/3
          JK1=JK1+1
  801 CONTINUE 
      GO TO 903
  901 CONTINUE 
      JK1=1
      DOK4=0.
      DOK3=0.
      DO 902 KK1=1,2*NMM-1,2
      DOK4=DOK4+(DDOK(KK1+1)**5.-DDOK(KK1)**5.)*ZX(JK1)/
     &									(DDOK(KK1+1)-DDOK(KK1))
      DOK3=DOK3+(DDOK(KK1+1)**4.-DDOK(KK1)**4.)*ZX(JK1)/
     &									(DDOK(KK1+1)-DDOK(KK1))
	DOKSD = DOKSD + GDOK(JK1)*
     &           (DDOK(KK1)**2+DDOK(KK1)*DDOK(KK1+1)+DDOK(KK1+1)**2)/3.0     
      JK1=JK1+1
 902  CONTINUE
      DOKM=0.8*DOK4/DOK3    
 903  CONTINUE
		DOKSD = DOKSD-DOKM**2
	CONTINUE  
c       #####################################################
 700  CONTINUE
	if (gsv.eq.3) then
		call random_seed
		call random_seed(get = Xseed)
		X = drand(Xseed(1))
	end if
c вывод 0% на экран:
	Ist = 0
	Its = Its + 1	
	call gettim(ihr,imin,isec,i100th)
	print'(1X,a,I2,a,I2,a,I2,a\)','[',IHR,' : ',IMIN,' : ',isec,'] '
	IF(IPRIS.EQ.1) THEN
	print'(a,I3,a\)','Cycle =',kpris+1,' '
	ELSE
	print'(a,I3,a\)','Cycle =',ipris,' '
	ENDIF
c Моделирование N базовых частиц с окружением
      do 10 I=1,N
	attempt = 0
c Моделирование i-й базовой частицы с окружением
  11    CONTINUE
	attempt = attempt + 1
	TU=12.56636
	fmkarm2_loc = 0
	ireset = 0
	ipocket_loc = 0
	ibridge_loc = 0
	ipocket_loc_cor = 0
	ibridge_loc_cor = 0
	ibridge_loc_cor2 = 0
	jammed_loc = 0
	idok_local_all = 0
	Vdok_loc = 0.
	Vmkm_loc = 0.
	Vdok_loc2 = 0.
	Vmkm_loc2 = 0.
	SVD1 = 0.
	VSMKM1=0.
c       #####################################################

      if(gsv.eq.2)then 
          CALL RANDOM2(X,u01,u11,u21,u31,u41,u51,u61,u71,u81,u91)
          CALL RANDOM2(X0,u02,u12,u22,u32,u42,u52,u62,u72,u82,u92)
      else
		if(gsv.eq.3)then
			X = drand(0)
			X0 = drand(0)		
		else
			X=RANDOM1(A1,B1)
			X0=RANDOM1(A2,B2)
		end if
      end if
      call SIZE(JZZ,NMM,X,X0,DDOK,Z11,Dr,Nfract)
	        XSS0=XSS0+X0
			XSS1=XSS1+X
			NFX=NFX+1
c		определение параметров распределения ДОК		
	ALLDOK_FRACT(Nfract) = ALLDOK_FRACT(Nfract) + 1
	ALLVDOK_FR(Nfract) = ALLVDOK_FR(Nfract) + 3.14159/6*(Dr**3)
		iks = int(Dr/Di)+1
			ALLDOK(iks) = ALLDOK(iks)+1
			ALLVDOK(iks) =  ALLVDOK(iks) + 3.14159/6*Dr**3
	DOK_base41 = DOK_base41 + Dr**4.
	DOK_base31 = DOK_base31 + Dr**3.
C		----------------------------------------
	if	(SFR(Nfract).eq.0) goto 11
      if(Dr.ge.Dmax) then
		conditions(1) = conditions(1) + 1
		goto 11
	endif
      if(Dr.le.Dmin) then
		conditions(2) = conditions(2) + 1
		goto 11
      endif
      Sd4=Sd4+Dr**4.
      Sd3=Sd3+Dr**3.
	VP=3.14159/6.*Dr**3.
c       #####################################################

 501    if(gsv.eq.2)then
          CALL RANDOM2(X1,u03,u13,u23,u33,u43,u53,u63,u73,u83,u93)
        else
			if(gsv.eq.3)then
				X1 = drand(0)		
			else
				X1=RANDOM1(A3,B3)
			end if
	  end if
        IF(X1.EQ.1.0) GO TO 501
        XSS2=XSS2+X1
        NFY=NFY+1
        KCIL=-(dlog(1-X1)*(1./sLamd))+VP
        VP=KCIL
        rrL=(3*KCIL/(4*3.14159))**(1/3.)
c       #####################################################
 345    if(gsv.eq.2)then
          CALL RANDOM2(X2,u04,u14,u24,u34,u44,u54,u64,u74,u84,u94)
          CALL RANDOM2(X21,u05,u15,u25,u35,u45,u55,u65,u75,u85,u95)
        else
			if(gsv.eq.3)then
				X2 = drand(0)
				X21 = drand(0)		
			else
				X2=RANDOM1(A4,B4)
				X21=RANDOM1(A5,B5)
			end if
        end if
      call SIZE(JZZ,NMM,X2,X21,DDOK,Z11,Db,Nfract)
	        XSS3=XSS3+X2
			XSS4=XSS4+X21
			NFZ=NFZ+1
c	определение параметров распределения ДОК
	ALLDOK_FRACT(Nfract) = ALLDOK_FRACT(Nfract) + 1
	ALLVDOK_FR(Nfract) = ALLVDOK_FR(Nfract) + 3.14159/6*(Db**3)
	iks = int(Db/Di)+1 
	ALLDOK(iks) = ALLDOK(iks)+1
	ALLVDOK(iks) =  ALLVDOK(iks) + 3.14159/6*(Db**3)
	DOK_sur41 = DOK_sur41 + Db**4.
	DOK_sur31 = DOK_sur31 + Db**3.
C	----------------------------------------
	if	(SFR(Nfract).eq.0) goto 501
	if(Dr.ge.Dmax) then
		conditions(1) = conditions(1) + 1
		goto 501
	endif
      if(Db.le.Dmin) then
		conditions(2) = conditions(2) + 1
		goto 501
      endif
	D41=D41+Db**4.
      D31=D31+Db**3.
	AA = rrl-Dr/2.-Db/2.
c	подсчет общего числа частиц окружения
      idok_local_all = idok_local_all + 1	
	vdokstr(iks) = vdokstr(iks) + 3.14159/6*(Db**3)
	qdokstr(iks) = qdokstr(iks) + 1
c
c			Проверка неравенства (1)
c       #####################################################
      If(Dr.Lt.(AK1*Db))then
		conditions(3) = conditions(3) + 1
		if (ivar.eq.0) GO TO 11
		GO TO 501
	endif 
      if(Dr.GT.(AK2*Db))then
		conditions(4) = conditions(4) + 1
		go to 501
      endif
c   
c			Проверка неравенства (2)
c       #####################################################
 551  CONTINUE
	iks = int((AA/max(Dr,Db)+1)*100) + 1
	if (iks.le.Nc) then
		coef(iks) = coef(iks)+1
		if (iks.gt.coef_nmax) coef_nmax = iks 
	endif
 	
      if(AA.gt.(max(Dr,Db)*AK4))then
		conditions(5) = conditions(5) + 1
		goto 11
	endif
      if(AA.gt.(max(Dr,Db)*AK3))goto 502
c	проверка условия непересечения частиц
      if(AA.le.0.0) then
		RRL=Dr/2.+Db/2.
		AA=0.
		jammed_loc = jammed_loc + 1
	endif
c       #####################################################		  
c			Определение характеристик МКМ
c       #####################################################
	ibridge_loc = ibridge_loc + 1
	IF(IPRIS.EQ.0) GO TO 550		
      RR1=AK3*Dr
      RR2=AK4*Dr
	mindk = int(RR1/Di)+1
	maxdk = int(RR2/Di)+1
 444  CONTINUE 
      AUS=0.
      DO 441 iks=mindk,maxdk
 441		AUS=AUS+QKS1(iks)       
      IF(AUS.EQ.0.)GO TO 11
      AUS=1./AUS
      DO 448 iks=1,Nkarm+1
		DPOC(iks)=0.
 448		FQKS(iks)=0.
      DPOC(mindk)=Di*mindk
      DO 445 iks=mindk,maxdk
		FQKS(iks+1)=AUS*QKS1(iks)+FQKS(iks)
		DPOC(iks+1)=Di+DPOC(iks)
 445  CONTINUE
      DO 446 iks=mindk,maxdk+1
          IF((1.0-FQKS(iks)).LT.1E-5) THEN
			maxdk=iks-1
			FQKS(iks)=1.
			GO TO 447
          END IF
 446  CONTINUE
 447  NNN1=maxdk-mindk+2
      DO 450 iks=1,NNN1
          DPOC(iks)= DPOC(mindk-1+iks)
 450      FQKS(iks)= FQKS(mindk-1+iks)

 451  if(gsv.eq.2)then
          CALL RANDOM2(X3,u06,u16,u26,u36,u46,u56,u66,u76,u86,u96)
      else
		if(gsv.eq.3)then
			X3 = drand(0)		
		else
			X3=RANDOM1(A6,B6)
		end if
      end if
      XSS5=XSS5+X3
      NFQ=NFQ+1
      CALL DM(X3,NNN1,FQKS,DPOC,DKARM)
      CALL VM(Dr/2.,Db/2.,DKARM/2.,AA,JJ,VMKM,BB)	    
      IF(JJ.EQ.0) GO TO 451
	
	if (AA.gt.0) then
		iks = int((AA/max(Dr,Db))*1000) + 1
		if (iks.le.Nc) then 
			qmkm1(iks) = qmkm1(iks) + 1
			if (iks.gt.qmkm1_nmax) qmkm1_nmax = iks
		endif
	endif

	iks = int((BB/max(Dr,Db))*100) + 1
	if (iks.le.Nc) then 
		qmkm2(iks) = qmkm2(iks) + 1
		if (iks.gt.qmkm2_nmax) qmkm2_nmax = iks
	endif	

c
	VSMKM1=VSMKM1+VMKM
	SVD1 = SVD1 + 3.14159/6.*Db**3.
c	Проверка наличия кармана в MKM
c	Var#1	 
	if (max(Dr,Db).le.Dmaxxx) then
		ibridge_loc_cor = ibridge_loc_cor + 1	
		Vdok_loc = Vdok_loc + 3.14159/6.*Db**3.
		Vmkm_loc = Vmkm_loc + VMKM
	endif
c	Var#2
	if(gsv.eq.2)then
          CALL RANDOM2(X4,u06,u16,u26,u36,u46,u56,u66,u76,u86,u96)
      else
		if(gsv.eq.3)then
			X4 = drand(0)		
		else
			X4=RANDOM1(A6,B6)
		end if
      end if
      XSS6=XSS6+X4
      NFW=NFW+1
	if(X4.LT.mkmcoef/karmcoef*pdoksmall(int(max(Dr,Db)/Di)+1))goto 550
		ibridge_loc_cor2 = ibridge_loc_cor2 + 1
		Vdok_loc2 = Vdok_loc2 + 3.14159/6.*Db**3.
		Vmkm_loc2 = Vmkm_loc2 + VMKM	
      goto 550
c		  
c			Определение характеристик "карманов"
c       #####################################################
 502  ipocket_loc = ipocket_loc + 1
	RK=AA
	if (RK.GT.DPmax) DPmax = RK
      if(RK.le.0.0)goto 333
      iks = int(RK/Di)+1
      QKS(iks)=QKS(iks)+1
	VKS(iks)=VKS(iks)+3.14159/6*(RK**3)
	DP31 = DP31 + RK**3.
	DP41 = DP41 + RK**4.
	IF (IPRIS.EQ.0) goto 333
c	Проверка наличия кармана в кармане
c	Var#1	
	if (pdoksmall(int(max(Dr,Db)/Di)+1).ge.1.) goto 333
		if (RK.GT.DPmax_cor) DPmax_cor = RK
		fmkarm_cor(iks) = fmkarm_cor(iks) + 3.14159/6*(RK**3)
		fqkarm_cor(iks) = fqkarm_cor(iks) + 1
c	Var#2	
	if(gsv.eq.2)then
          CALL RANDOM2(X4,u06,u16,u26,u36,u46,u56,u66,u76,u86,u96)
      else
		if(gsv.eq.3)then
			X4 = drand(0)		
		else
			X4=RANDOM1(A6,B6)
		end if
      end if
      XSS6=XSS6+X4
      NFW=NFW+1
	if (X4.LT.pdoksmall(int(max(Dr,Db)/Di)+1)) goto 333
		if (RK.GT.DPmax_cor) DPmax_cor = RK
		ipocket_loc_cor = ipocket_loc_cor + 1
		fmkarm2_loc(iks) = fmkarm2_loc(iks) + 3.14159/6*(RK**3) 	 		
 333  continue
c Определение параметров условной функции распределения ДОК
	DPRow = int(RK / Dj) + 1
	QDOK(DPRow) = QDOK(DPRow) + 1
	DOKP41(DPRow) = DOKP41(DPRow) + max(Dr,Db)**4.
	DOKP31(DPRow) = DOKP31(DPRow) + max(Dr,Db)**3.
	iks = int(max(Dr,Db)/Di)+1
	VDOKS(DPRow,iks) = VDOKS(DPRow,iks) + 3.14159/6*((Di*(iks-.5))**3)
	QDOKS(DPRow,iks) = QDOKS(DPRow,iks) + 1
c		Анализ локальной структуры
c #############################################################
550   CONTINUE 
      TU=TU-(Db**2./rrL**2.)
      IF(TU.gt.(12.56636/200.)) GO TO 501
		QKSS = sum(QKS)			
		QKS1=QKS/real(QKSS)
	IF(IPRIS.EQ.1) THEN	
		    fmkarm2 = fmkarm2 + fmkarm2_loc
c	ограничения
		nn = real(ipocket_loc)/real(ibridge_loc)
		IF (ipocket_loc.EQ.0) THEN
			conditions(6) = conditions(6) + 1
		    ireset = ireset + 1
		ENDIF
		IF (ibridge_loc.LT.2) THEN
			conditions(7) = conditions(7) + 1
			ireset = ireset + 1
		ENDIF
		IF (nn.LE.nn_min) THEN
			conditions(8) = conditions(8) + 1
			ireset = ireset + 1
		ENDIF
		IF (nn.GE.nn_max) THEN
			conditions(9) = conditions(9) + 1
			ireset = ireset + 1
		ENDIF
		IF (ireset.gt.0) GO TO 11
c	-----------
		jammed_total = jammed_total + real(jammed_loc)
     &									/real(ipocket_loc + ibridge_loc)
		nn_total = nn_total + nn	  
		ibridge_total = ibridge_total + ibridge_loc			
		iks = int(Dr/Di)+1				
		VSMKM(iks)=VSMKM(iks)+VSMKM1
		SVD(iks) = SVD(iks) + alpha*SVD1 + 3.14159/6.*Dr**3.
		IF (ibridge_loc_cor.gt.0) THEN
		  Vmkm_total(iks)=Vmkm_total(iks)+Vmkm_loc		           
		  Vdok_total(iks)=Vdok_total(iks)+alpha*Vdok_loc+3.14159/6.*Dr**3.
		ENDIF
		IF (ibridge_loc_cor2.gt.0) THEN
			Vmkm_total2=Vmkm_total2+Vmkm_loc2
			Vdok_total2=Vdok_total2+alpha*Vdok_loc2+3.14159/6.*Dr**3.
		ENDIF
	END IF
c---------вывод точек на экран:
	Ist = Ist + 1
	if(i.eq.N) then
	write(*,'(a,I3,a)')' ',int(100*real(Its)/(real(KXX)+1)),'% done'
	else
		if (Ist.GE.N/30) then
			Ist = 0
			write(*,'(a\)')'.'
		endif
	endif	
  10	continue
c#####################################################
c	Обработка статистики
c#####################################################      
	XSR0=XSS0/NFX
      EPS1=ABS((XSR0-0.5)/0.5)
      XSR1=XSS1/NFX
      EPS2=ABS((XSR1-0.5)/0.5)
      XSR2=XSS2/NFY
      EPS3=ABS((XSR2-0.5)/0.5)
      XSR3=XSS3/NFZ 
      EPS4=ABS((XSR3-0.5)/0.5)
      XSR4=XSS4/NFZ
      EPS5=ABS((XSR4-0.5)/0.5)
      XSR5=XSS5/NFQ
      EPS6=ABS((XSR5-0.5)/0.5)
      XSR6=XSS6/NFW
      EPS7=ABS((XSR6-0.5)/0.5)

C	Определение параметров распределения ДОК
C ########################################################
c	Параметры частиц ДОК, формирующих структуру 	      
	DOK43b=Sd4/Sd3
      DOK43s=D41/D31
c
	ALLDOKQ = sum(ALLDOK)
C		определение точности воспроизведения функции распределения ДОК по фракциям
	do kilo = 1,NMM
		epsalldok(kilo) = real(alldok_fract(kilo))/ALLDOKQ
		epsalldok(kilo) = abs(epsalldok(kilo) - zx(kilo))/zx(kilo)
	enddo
	ALLVDOKS = sum(ALLVDOK)
	ALLDOK432 = 0.
	ALLDOK243 = 0.
	FMDOK(1) = 0.
	do kilo = 1,Ndok
c	    определение массовой функции плотности распределения ДОК
		ALLVDOKSO(kilo)=ALLVDOK(kilo)/ALLVDOKS
		ALLDOK432=ALLDOK432+ALLVDOKSO(kilo)*(kilo-0.5)*Di
		ALLDOK243=ALLDOK243+ALLVDOKSO(kilo)*((kilo-0.5)*Di)**2
c	    определение массовой функции распределения ДОК
		FMDOK(kilo+1) = (FMDOK(kilo) + ALLVDOKSO(kilo))
	enddo
	ALLDOK43 = (dok_base41+dok_sur41)/(dok_base31+dok_sur31)
c	Standard Deviation of mass distribution:
	ALLDOKsd= ALLDOK243-ALLDOK432**2
c	Errors:
	EPSX1=ABS((DOKM - DOK_base41 / Dok_base31) / DOKM)
      EPSX2=ABS((DOKM - DOK_sur41 / Dok_sur31) / DOKM)
	EPSX3 = ABS((DOKM - ALLDOK43)/DOKM)
	if (EPSX3.gt.eps_dok) then
	write(*,'(1x,a,F6.4)')'WARNING! Dok accuracy low, err =',EPSX3
	end if
c
C	Определение условных функций распределения размеров 
C		ДОК по размерам "карманов" (по разрядам)
C ########################################################
c определение начального количесва разрядов:	
	DPRow = int(DPmax/Dj) + 1
c создание вектора размеров "карманов"	
	do irow = 1,DPRow
		Dpockets(irow) = irow*Dj
	enddo
600	MDOK3 = 0.
	MDOK4 = 0.
	DDOK3 = 0.
	DDOK4 = 0.
      QDOKSS = 0
	VVDOKS = 0.
	DOKP432 = 0.
	qdokkarm = 0.
	do irow = 1,DPRow
c определение QDOKSS и QDOKS1
	do iks= 1,Ndok
		QDOKSS(irow) = QDOKSS(irow) + QDOKS(irow, iks)
	enddo

	do iks = 1,Ndok
		if (QDOKSS(irow) ==0)then
		QDOKS1(irow, iks) = 0.
		else
		QDOKS1(irow, iks) = real(QDOKS(irow, iks))/real(QDOKSS(irow))
		endif
	enddo
c определение DOKP431
	do iks = 1,Ndok
		MDOK4(irow)=MDOK4(irow)+(Di*(Iks-0.5))**4.*QDOKS1(irow, iks)
		MDOK3(irow)=MDOK3(irow)+(Di*(Iks-0.5))**3.*QDOKS1(irow, iks)
	enddo
	if (MDOK3(irow).le.1e-30) then
		DOKP431(irow) = 0.
	else
		DOKP431(irow) = MDOK4(irow)/MDOK3(irow)
	endif
c определение погрешностей
	do iks = 1,Ndok
	DDOK4(irow)=DDOK4(irow) + 
     &			((Di*(Iks-0.5))**4.-MDOK4(irow))**2*QDOKS1(irow, iks)
     	DDOK3(irow)=DDOK3(irow) + 
     &			((Di*(Iks-0.5))**3.-MDOK3(irow))**2*QDOKS1(irow, iks) 
	enddo
	
	if(QDOKSS(irow)==0.or.MDOK3(irow).le.1e-30.or.
     &										MDOK4(irow).le.1e-30)then
		Epsydok(irow) = 0.
		Epsmdok3(irow) = 0.
	    Epsmdok4(irow) = 0.
	else
	Epsydok(irow)=3.*SQRT((1./real(QDOKSS(irow)))*
     &	((DDOK4(irow)/MDOK4(irow)**2)+(DDOK3(irow)/MDOK3(irow)**2)))
	Epsmdok3(irow) = (3./MDOK3(irow))*
     &						sqrt((1./real(QDOKSS(irow)))*DDOK3(irow))
	Epsmdok4(irow) = (3./MDOK4(irow))*
     &						sqrt((1./real(QDOKSS(irow)))*DDOK4(irow))
	endif

c определение VDOKSO и DOKP432	
	do iks = 1,Ndok
		VVDOKS(irow) = VVDOKS(irow) + VDOKS(irow,iks)
	enddo
	do iks = 1,Ndok
		if (VVDOKS(irow).le.1e-30) then
			VDOKSO(irow,iks)= 0.
		else
			VDOKSO(irow,iks)=VDOKS(irow,iks)/VVDOKS(irow)
		endif
		DOKP432(irow)=DOKP432(irow)+VDOKSO(irow,iks)*Di*(iks-0.5)
		if (QDOKSS(irow).eq.0) then
			QDOKSO(irow,iks)= 0.
		else
			QDOKSO(irow,iks)=QDOKS(irow,iks)/real(QDOKSS(irow))
		endif
		qdokkarm(irow) = qdokkarm(irow)+QDOKSO(irow,iks)*Di*(iks-0.5)
	enddo
c определение DOKP43
	if (DOKP31(irow).le.1e-30) then
		DOKP43(irow) = 0.
	else
		DOKP43(irow) = DOKP41(irow)/DOKP31(irow)
	endif

	enddo
C		Проверка точности (объединение разрядов)
C ######################################
	if (DPRow.GT.1) then
		shift_flag = 0
	do irow = 1,DPRow-1
	if (shift_flag == 0) then
		if (epsydok(irow).GT.eps_dok) then
c Объединение соседних разрядов:
			Dpockets(irow) = Dpockets(irow+1)
			DOKP41(irow) = DOKP41(irow) + DOKP41(irow+1)
			DOKP31(irow) = DOKP31(irow) + DOKP31(irow+1)
			QDOK(irow) = QDOK(irow) + QDOK(irow+1)
			do iks = 1,Ndok
			VDOKS(irow,iks) = VDOKS(irow,iks) + VDOKS(irow+1,iks)
			QDOKS(irow,iks) = QDOKS(irow,iks) + QDOKS(irow+1,iks)
			enddo
			shift_flag = 1
	     endif
	else
c Сдвиг разрядов влево:
			Dpockets(irow) = Dpockets(irow+1)
			DOKP41(irow) = DOKP41(irow+1)
			DOKP31(irow) = DOKP31(irow+1)
			QDOK(irow) = QDOK(irow+1)
			do iks = 1,Ndok
			VDOKS(irow,iks) = VDOKS(irow+1,iks)
			QDOKS(irow,iks) = QDOKS(irow+1,iks)
			enddo		
	endif
	enddo
	if (shift_flag /= 0) then
c Уменьшение общего количества разрядов на 1	
		DPRow = DProw - 1
c Повтор расчета
		go to 600
	endif
	endif
c Проверка последнего разряда:
	if ((epsydok(DPRow).GT.eps_dok).and.(DPRow.GT.1)) then
c Объединение с предыдущим разрядом:
		Dpockets(DPRow-1) = Dpockets(DPRow)
		DOKP41(DPRow-1) = DOKP41(DPRow-1) + DOKP41(DPRow)
		DOKP31(DPRow-1) = DOKP31(DPRow-1) + DOKP31(DPRow)
		QDOK(DPRow-1) = QDOK(DPRow-1) + QDOK(DPRow)
		do iks = 1,Ndok
		VDOKS(DPRow-1,iks) = VDOKS(DPRow-1,iks) + VDOKS(DPRow,iks)
		QDOKS(DPRow-1,iks) = QDOKS(DPRow-1,iks) + QDOKS(DPRow,iks)
		enddo
c Уменьшение общего количества разрядов на 1
		DPRow = DProw - 1
	endif
C ##################################################################
C	Определение параметров распределения "карманов"
C ################################################################## 
      md3=0.
      md4=0.
      dd3=0.
      dd4=0.	
      DO 605 Iks=1,Nkarm
          MD4=MD4+(Di*(Iks-0.5))**4.*QKS1(Iks)
          Md3=MD3+(Di*(Iks-0.5))**3.*QKS1(Iks)
 605  CONTINUE        
      DO 606 Iks=1,Nkarm
          DD4=DD4+((Di*(Iks-0.5))**4.-MD4)**2*QKS1(Iks)
          DD3=DD3+((Di*(Iks-0.5))**3.-MD3)**2*QKS1(Iks)
 606  CONTINUE
      EPSY=3.*SQRT((1./QKSS)*((DD4/MD4**2)+(DD3/MD3**2)))
	if (EPSY.gt.eps_dok) then
	write(*,'(1x,a,F6.4)')'WARNING! Pockets accuracy low, err = ',EPSY
	end if
      D43=MD4/MD3
      epsMD4=(3./MD4)*sqrt((1./QKSS)*DD4)
      epsMD3=(3./MD3)*sqrt((1./QKSS)*DD3)
      VVKS=0.
      do 23 kilo=1,Nkarm
		VVKS=VVKS+VKS(kilo)
 23   continue
      D432=0.; D243=0.; 	Dqkarm = 0.;
      DO 601 kilo=1,Nkarm
          VKSO(kilo)=VKS(kilo)/VVKS
          D432=D432+VKSO(kilo)*DI*(kilo-0.5)
		D243 = D243 + VKSO(kilo)*(DI*(kilo-0.5))**2
		Dqkarm = Dqkarm + qks1(kilo)*DI*(kilo-0.5)
 601  CONTINUE
c	Mass-medium size:  
	DP43 = DP41/DP31
c	Standard Deviation of mass distribution:
	sdevP43 = (D243 - D432**2)**0.5
	
	
c #############################################################
c	Массовая доля док' в составе топлива
	gdokleft = FMDOK(int(Dmin/Di)+1)*GGG
	gdoksfr=0.
	do kilo = 1,NMM
		if (SFR(kilo).eq.0) gdoksfr = gdoksfr + gdok(kilo)*GGG
	enddo
	gdokleft = gdokleft + gdoksfr

	PLOTsmdok = (1.-(GGG-gdokleft))/(1./PLOT2-(GGG-gdokleft)/PLOT1)
c	Объемная доля ДОК в составе композиции св-м-док'
		vdokleft=gdokleft/(1.-GGG+gdokleft)*PLOTsmdok/PLOT1
	PLOTsm = (1.-GGG)/(1./PLOT2-GGG/PLOT1)

c 	 Коэффициент для определения Daggl
	mp=3.14159/6*PLOTsmdok * Gm/(1-GGG+gdokleft)
	mp=mp*(1.+3.*0.016*eta/(2.*0.027 + 3.*0.016*(1-eta)))
	mp=2.*(0.75/3.14159*mp*((1-eta)/2000. + eta/3000.))**0.3333



c #############################################################
c 	Доля  ДОК < 0.5 Dбаз
	ddoksmall=0
	zdoksmall=0
	do kilo = 1,Ndok
		Xr = mod(real(kilo),ak2)/ak2
		if (real(real(kilo)/ak2).lt.1) goto 824
		do iks = 1,int(real(kilo)/ak2)
			zdoksmall(kilo)=zdoksmall(kilo)+vdokstr(iks)/sum(vdokstr)
		enddo
 824		continue
	zdoksmall(kilo)=zdoksmall(kilo)+vdokstr(int(real(kilo)/ak2)+1)/
     &                                                  sum(vdokstr)*Xr
c	Определение (Dдок<Dбаз)43
		if (zdoksmall(kilo).gt.1e-5) then
		do iks = 1,int(real(kilo)/ak2)
			ddoksmall(kilo)=ddoksmall(kilo)+vdokstr(iks)/sum(vdokstr)/
     &									zdoksmall(kilo)*DI*(iks-0.5)
		enddo
	ddoksmall(kilo)=ddoksmall(kilo)+vdokstr(int(real(kilo)/ak2)+1)*Xr/
     &      sum(vdokstr)/zdoksmall(kilo)*DI*(int(real(kilo)/ak2)+0.5*Xr)
		end if
	continue
	enddo


c	Массовая доля ДОК < 0.5 Dбаз в топливе
	zdoksmall=zdoksmall*ggg
	do kilo = 1,Ndok
c	плотность композиции св-м-док'+мелк.док
	Pl = (1.-(GGG-gdokleft-zdoksmall(kilo)))/
     &			(1./PLOT2-(GGG-gdokleft-zdoksmall(kilo))/PLOT1)
c	Объемная доля ДОК < 0.5 Dбаз в композиции св-м-док'+мелк.док
	vdoksmall(kilo)=zdoksmall(kilo)/(1-GGG+gdokleft+zdoksmall(kilo))*
     &PL/PLOT1
c	Отношение (Dдок<Dбаз)43 / Dбаз
	ddoksmall(kilo) = ddoksmall(kilo)/(Di*kilo)
	enddo
c	Определение вероятности наличия мелких док в карманах вокруг базовой док
	do kilo = 2,Ndok
c	pdoksmall(kilo) = vdoksmall(kilo)*ddoksmall(kilo)*DokM*1e6/20.7
	pdoksmall(kilo) = vdoksmall(kilo)*ddoksmall(kilo)*karmcoef
		if((pdoksmall(kilo).lt.pdoksmall(kilo-1))) then
			pdoksmall(kilo)=pdoksmall(kilo-1)
		endif
	enddo
	Dmaxxx = Ddokmax
c	определение максимального размера док и доли сверхкрупных ДОК
	do kilo = 1,Ndok
		if (mkmcoef/karmcoef*pdoksmall(kilo).ge.1.) then
			Dmaxxx = Di*kilo
			zdmaxxx = 1 - FMDOK(kilo+1)
			exit
		endif
	enddo	

c #############################################################
c Прерывание на цикле 0
	IF(IPRIS.EQ.0) THEN
		write(*,*)
		IPRIS=IPRIS+1
		GO TO 700
      END IF

c #############################################################
c Цикл >= 1
c	Параметры распределения карманов с учетом коррекции
	Dkarm43_cor = 0.;Dkarm243_cor = 0.; Dqkarm_cor = 0.;
	do kilo = 1, Nkarm	
		Dkarm43_cor = Dkarm43_cor + fmkarm_cor(kilo)/sum(fmkarm_cor)
     &	*DI*(kilo-0.5)
		Dkarm243_cor = Dkarm243_cor + fmkarm_cor(kilo)/sum(fmkarm_cor)
     &	*(DI*(kilo-0.5))**2
		Dqkarm_cor = Dqkarm_cor + real(fqkarm_cor(kilo))
     &	/real(sum(fqkarm_cor))*DI*(kilo-0.5)
	enddo
c	Standard Deviation of mass distribution:
	sdevP43_cor = (Dkarm243_cor - Dkarm43_cor**2)**0.5
c	второй вариант ф-и плотности распределения "карманов"
	Dfmk432 = 0.; Dfmk243 = 0.
	do kilo = 1, Nkarm
		Dfmk432 = Dfmk432 + fmkarm2(kilo)/sum(fmkarm2)*DI*(kilo-0.5)
		Dfmk243 = Dfmk243 + fmkarm2(kilo)/sum(fmkarm2)*(DI*(kilo-0.5))**2
	enddo
c	Standard Deviation of mass distribution:
	sdevP243 = (Dfmk243 - Dfmk432**2)**0.5

c	Размеры МКМ
	Dqmkm1 = 0.; Dqmkm2 = 0.; qmcoef = 0.
	do kilo = 1, Nc
		Dqmkm1=Dqmkm1 + real(qmkm1(kilo))/real(sum(qmkm1))*0.001*(kilo-0.5)
		Dqmkm2=Dqmkm2 + real(qmkm2(kilo))/real(sum(qmkm2))*0.01*(kilo-0.5)
		qmcoef=qmcoef + real(coef(kilo))/real(sum(coef))*0.01*(kilo-0.5)
	enddo
C  ############################################################
c			Расчет доли "карманов" в составе 
c	                         композиции связующее-металл
c  ############################################################
c-----распределение доли карманов по размерам базовых док:
	do kilo = 1,Ndok
			sumvsmkm = 0
			sumsvd = 0
			sumvmkmt = 0
			sumvdokt = 0				
		do iks = 1,kilo
			sumvsmkm = sumvsmkm + VSMKM(iks)
			sumsvd = sumsvd + SvD(iks)
			sumvmkmt = sumvmkmt + Vmkm_total(iks)
			sumvdokt = sumvdokt + Vdok_total(iks)
		enddo
		gk0(kilo)=1-sumvsmkm*PLOTsmdok*(GGG-gdokleft)/	
     &						(sumsvd*PLOT1*(1.-GGG+gdokleft))
		gk1(kilo)=1-sumvmkmt*PLOTsmdok*(GGG-gdokleft)/
     &						(sumvdokt*PLOT1*(1.-GGG+gdokleft))
	enddo
c--------
c      	Вариант№1	
	DolM1=sum(VSMKM)*PLOTsmdok*(GGG-gdokleft)/
     &						(sum(SvD)*PLOT1*(1.-GGG+gdokleft))
c--------	
	Vdok_total0 = sum(Vdok_total) + sum(Vmkm_total)*vdokleft
	Vdok_total1 = sum(Vdok_total)/(1-gdokleft/GGG)
	deltaVdok = (Vdok_total1 - Vdok_total0)/Vdok_total1
c--------
c      	Вариант№2
	Vdok_total = Vdok_total/(1-gdokleft/GGG)
	DolM2=sum(Vmkm_total)*(1-vdokleft)/
     &		(sum(Vdok_total)*plot1*(1-GGG)/GGG/plotsm)
c--------
c      	Вариант№3
	Vdok_total2 = Vdok_total2/(1-gdokleft/GGG)
	DolM3=Vmkm_total2*(1-vdokleft)/
     &		(Vdok_total2*plot1*(1-GGG)/GGG/plotsm)

	IF(IPRIS.EQ.1.AND.KPRIS.EQ.0) FI=I-1
	KPRIS=KPRIS+1
C  ############################################################
c			Вывод
c  ############################################################
c	Вывод файла .tab       
c	if(kpris.eq.1)then
c		write(3,*)'         FI      EPSX1          EPSX2
c     &        Точность2         D43' 
c          write(9,*)'         FI     Точность2        RRL  
c     &         EPSDOK430      EPSDOK4310'
c      end if
c      write(3,*)fi,epsx1,epsx2,epsy,d43
c      write(9,*)fi,epsy,epsdok430,epsdok4310
	if (KXX.GT.1) then
		par1(kpris)=epsy
		par2(kpris)=epsmd3
		par3(kpris)=epsmd4	
		par4(kpris)=(ALLDOK43-DokM)/DokM
		par5(kpris)=(ALLDOKsd-DokSD)/DokSD
		par6(kpris)=1-DolM2
	end if
c Завершение моделирования цикла
	write(*,*) 
      IF(IPRIS.EQ.1.AND.KPRIS.LT.KXX) THEN
		FI=FI+N
          GO TO 700
      END IF
c			Вывод результатов моделирования в файл .m
c       #####################################################
	call gettim(ihr,imin,isec,i100th)
	print'(1X,a,I2,a,I2,a,I2,a)','[',IHR,' : ',IMIN,' : ',isec,'] 
     &Writing the output file'
	write(4,*)'clear fqdokkarm'     
      write(4,*)'% ===================================================='
	write(4,*)'% Input filename: ', trim(FLDN)//'.dat'
	write(4,*)'% ===================================================='
	write(4,925)'% Plot1 = ',PLOT1,'; Plot2 = ',PLOT2,'; Gdok = ',GGG0,'; 
     &Gm = ',Gm,';'
925	format(1x,a,F6.1,a,F6.1,a,F5.3,a,F5.3,a)
      write(4,926)'% Nfr =',NMM,'; JZ =',JZZ,'; Cycles =',KXX,'; N =',N
926	format(1x,a,I3,a,I2,a,I3,a,I7\)
      if(gsv.ne.1)then
	write(4,'(a,I2,a)')'; GSV =',gsv,';'
      else
	write(4,'(a,I2,a,E9.3,a)')'; GSV =',gsv,'; NNZ =',NNZ,';'        
      end if
	call arrayprint('Gfr',GDOK,NMM)
	call arrayprint('Dfr',DDOK,2*NMM)
	write(4,*)'%_____________________________________________________'
	write(4,*)' Dmin =',int(Dmin*1.00001e6),'; % mkm'
	write(4,*)' Di =',int(Di*1.00001e6),'; % mkm'
	write(4,*)' Dj =',int(Dj*1.00001e6),'; % mkm'
	write(4,*)'% Nkarm/Nmkm(min, max) = [',NN_min, NN_max,'];'
	write(4,*)'% k5 =',alpha,';'
	write(4,*)'% Statistical significance P(alpha)=',alfa,';'
	write(4,*)'% eps =',eps_dok,';'
	write(4,*)'% Calculation variant =',ivar,';'
	write(4,*)'% P(karm-in-karm) coef =',karmcoef,';'
	write(4,*)'% P(karm-in-MKM) coef =',mkmcoef,';'
	write(4,*)'% Zok* =',gdokns,';'
	do iks = 1,NMM
		if (SFR(iks).eq.0) then
			write(4,*)'% Fractions used to form pockets:'
			call arrayprint('sfr',real(SFR),NMM)
			exit
		endif
	enddo
      write(4,*)
	write(4,*)
      write(4,*)'% ===================================================='
      write(4,*)'%				Calculation	Results                  '
      write(4,*)'% ===================================================='
	write(4,*)
      write(4,*)'% Cycles:',KPRIS
      write(4,*)'% Total basic particles number:'
	write(4,*)'Nbase =', N,'+',FI,';'
      write(4,*)'% Total defined pockets number:'
	write(4,*)'Nkarm =', QKSS,';'
      write(4,*)'% Total calls of random generator:'
      write(4,*)'NFX =',NFX,';  NFY =',NFY,';'
	write(4,*)'NFQ =',NFQ,';  NFW =',NFW,';'
      write(4,*)'% Accuracy of random numbers modeling :'
      write(4,*)'epsx(1)=',EPS1,'; epsx(2)=',EPS2,'; epsx(3)=',EPS3,';' 
      write(4,*)'epsx(4)=',EPS4,'; epsx(5)=',EPS6,'; epsx(6)=',EPS7,';'
	write(4,*)
	write(4,*)'%_______________Local structure analysis______________'
	write(4,*)'% Medium coef [(Lij/Ddok)+1]:', qmcoef, ';'
	write(4,*)'% Medium number of bridges:', real(ibridge_total)/(FI)
	write(4,*)'% Medium ratio between number of pockets and bridges :',
     &	 nn_total/(FI)
	write(4,*)'% Medium fraction of paricles number in jammed pack:',
     &	 jammed_total/(FI)
	write(4,*)'% Medium number of conditions breakings:'
	write(4,*)'%    1) Dok > Dmax      :', real(conditions(1))/(FI+N)
	write(4,*)'%    2) Dok < Dmin      :', real(conditions(2))/(FI+N)
	write(4,*)'%    3) Dbase < 0.5Dok  :', real(conditions(3))/(FI+N)
	write(4,*)'%    4) Dbase > 2.0Dok  :', real(conditions(4))/(FI+N)
	write(4,*)'%    5) l > 4.7Dbase    :', real(conditions(5))/(FI+N)
	write(4,*)'%    6) Nkarm = 0       :', real(conditions(6))/FI
	write(4,*)'%    7) Nmkm < 2        :', real(conditions(7))/FI
	write(4,*)'%    8) Nkarm/Nmkm < min:', real(conditions(8))/FI
	write(4,*)'%    9) Nkarm/Nmkm > max:', real(conditions(9))/FI
      write(4,*)
	write(4,*)'%___________________Dok parameters____________________'
	write(4,*)'% Mass-medium diameter of all Dok particles, mkm:'
913	format(1x,a,F7.2,a)
	write(4,913)'Dok43a = ',DOKM*1e6,
     &'; %(analytical calculation, all particles)'
      write(4,'(1x,a,F7.2,a,F7.2,a)')'Dok43all = [',ALLDOK43*1e6,' ',  
     &ALLDOK432*1e6,']; %(all particles, 2 variants)'
      write(4,913)'Dok43(1) = ',DOK43b*1e6,'; %(basic only)'
      write(4,913)'Dok43(2) = ',DOK43s*1e6,'; %(surrounding only)'
	write(4,*)'% Accuracy of all Dok particles sizes modeling:'
      write(4,*)'epsalldok =',EPSX3, ';'
	write(4,*)'% Accuracy of basic and surrounding Dok particles sizes
     & modeling:'
      write(4,*)'epsdok(1) =',EPSX1,'; epsdok(2) =',EPSX2,';'
	if (alfa.GT.0) then
	write(4,913)'Dokb_max = ',Dmax*1e6,
     &'; %(maximum size of basic particles)'
	endif
	write(4,913)'Ddok_max = ',Ddokmax*1e6,
     &'; %(maximum size of all particles)'
	write(4,*)'% Standard deviation of all Dok particles, mkm:'
	write(4,913)'Dok43sd = ',DOKSD**0.5*1e6,
     &'; %(analytical calculation, all particles)'
c	write(4,*)'% Mass density distribution function of <fractions>:'
c	call arrayprint('fmfract',ALLVDOK_Fr/ALLvDOKS,NMM)
	write(4,*)'% Accuracy of Dok fractions distribution function modeling:'
	call arrayprint('epsdokfr',EPSALLDOK,NMM)
	write(4,*)'% Total fraction of homogenized oxidizer:'
	write(4,*)'fineoxy_fr =', gdokns + gdokleft/GGG, ';'
      write(4,*)
	write(4,*)'%________________Pockets parameters___________________' 
      write(4,*)'% Accuracy of pockets distribution function 
     &modeling:'
	write(4,*)'epsfkarm =',EPSY,';'
      write(4,*)'% Accuracy of pockets distribution moments 
     &determination (4-th and 3-th):'
      write(4,*)'epsmkarm(1)=',epsMD4,'; epsmkarm(2)=',epsMD3,';'
      write(4,*)'% Mass-medium size of pockets (var. #1), mkm:'
      write(4,913)'Dkarm43(1) = ',DP43*1e6,';'
	write(4,*)'% Mass-medium size of pockets (var. #2)
     &and standard deviation, mkm:'
 946  format(1x,a,F7.2,a,F7.2,a,F6.3,a)
	write(4,946)'Dkarm43(2) = ',D432*1e6,'; Dkarm43sd = ',SDEVP43*1e6,
     &'; %(' ,SDEVP43/D432,' relative)'
	write(4,*)'% Cor. mass-medium size of pockets
     & and standard deviation (cor. var. #1), mkm:'
	write(4,946)'Dkarm43_cor(1) = ',Dkarm43_cor*1e6,'; Dkarm43sd_cor(1) =',
     &SDEVP43_cor*1e6,'; %(' ,SDEVP43_cor/Dkarm43_cor,' relative)'
	write(4,*)'% Cor. mass-medium size of pockets
     & and standard deviation (cor. var. #2), mkm:'
	write(4,946)'Dkarm43_cor(2) = ',Dfmk432*1e6,'; Dkarm43sd_cor(2) =',
     &SDEVP243*1e6,'; %(' ,SDEVP243/Dfmk432,' relative)'
	write(4,*)'% Medium size of pockets, mkm:'
	write(4,'(1x,a,F7.2,a)')'Dkarm10 = ',Dqkarm*1e6,';'
	write(4,*)'% Cor. medium size of pockets (cor. var. #1), mkm:'
	write(4,'(1x,a,F7.2,a)')'Dkarm10_cor = ',Dqkarm_cor*1e6,';'
	write(4,*)
	write(4,*)'% <Zkarm>'
	write(4,*)'% Mass fraction of pockets in <binder-metal> 
     &composition (not corrected):'
      write(4,*)'Zkarm = ', 1.-dolm1, ' ;'
	write(4,*)'% Cor. (var. #1, #2) mass fraction of pockets  
     &in <binder-metal> composition:'
      write(4,*)'Zkarm_cor = [', 1-dolm2, 1-dolm3,' ];'
	write(4,*)
	write(4,*)'% <Agglomerates>'
	write(4,*)'% Pocket to Agglomerate size coefficient:' 
      write(4,*)'da_coef = ',mp,';'
	write(4,*)'% Mass medium size of agglomerates
     & (normal var. #1,2  and cor. var. #1,2):' 
	write(4,'(1x,a,F7.2,a)')'Dagg43(1) = ',DP43*1e6*mp,';'
	write(4,'(1x,a,F7.2,a)')'Dagg43(2) = ',D432*1e6*mp,';' 
	write(4,'(1x,a,F7.2,a)')'Dagg43_cor(1) = ',Dkarm43_cor*1e6*mp,';' 
	write(4,'(1x,a,F7.2,a)')'Dagg43_cor(2) = ',Dfmk432*1e6*mp,';'
	write(4,*)
	write(4,*)'%___________Interpocket bridges parameters____________' 
	write(4,*)'% Medium MKM/Dok size between Dok particles:'
	write(4,'(1x,a,F6.4,a)')'Dqmkm1 = ',Dqmkm1,';'
	write(4,*)'% Medium MKM/Dol size between pockets:'
	write(4,'(1x,a,F6.4,a)')'Dqmkm2 = ',Dqmkm2,';'
	write(4,*)
	write(4,*)
      write(4,*)'%__________________Functions__________________________'
	write(4,*)
	write(4,*)'% <Dok>'
	write(4,'(1x,a,I3,a)')'% Mass density distribution function of all 
     &dok <particles> sizes (step =',int(Di*1.00001e6),'mkm)'
	call arrayprint('fmdok',ALLVDOKSO/(Di*1e6),Ndok)
	write(4,*)
	write(4,*)'% <Pockets>'
	write(4,'(1x,a,I3,a)')'% Mass density distribution function of <pocket> 
     &sizes (step =',int(Di*1.00001e6),'mkm):' 
      call arrayprint('fmkarm',VKSO/(Di*1e6),int(DPmax/Di) + 2)	
	write(4,'(1x,a,I3,a)')'% Cor. mass density distribution function of 
     &<pocket> sizes (step =',int(Di*1.00001e6),'mkm)(cor. var #1):' 
      call arrayprint('fmkarm_cor',fmkarm_cor/sum(fmkarm_cor)/(Di*1e6),
     &int(DPmax_cor/Di) + 2)	
	write(4,'(1x,a,I3,a)')'% Cor. mass density distribution function of  
     &<pocket> sizes (step =',int(Di*1.00001e6),'mkm)(cor. var#2):' 
      call arrayprint('fmkarm_cor2',fmkarm2/sum(fmkarm2)/(Di*1e6),
     &int(DPmax_cor/Di) + 2)
	write(4,'(1x,a,I3,a)')'% Numeric density distribution function of  
     &<pocket> sizes (step =',int(Di*1.00001e6),'mkm):'
      call arrayprint('fqkarm',QKS1/(Di*1e6),int(DPmax/Di) + 2)
	write(4,'(1x,a,I3,a)')'% Cor. numeric density distribution function of  
     &<pocket> sizes (step =',int(Di*1.00001e6),'mkm)(cor. var #1):'
      call arrayprint('fqkarm_cor',real(fqkarm_cor)/sum(fqkarm_cor)/
     &(Di*1e6),int(DPmax_cor/Di)+2)
	write(4,*)
	write(4,*)'% <MKM/Dok>'
	write(4,*)'% Density distribution function of MKM/Dok between Dok 
     &particles (step = 0.01)'
	call arrayprint('fqmkm1',real(qmkm1)/sum(qmkm1),qmkm1_nmax + 2)
	write(4,*)'% Density distribution function of MKM/Dok between 
     &pockets (step = 0.01)'
	call arrayprint('fqmkm2',real(qmkm2)/sum(qmkm2),qmkm2_nmax + 2)
	write(4,*)
	write(4,*)'% <Local structure analysis>'
	write(4,*)'% Coefficient [(Lij/Ddok)+1] distribution (step = 0.01):'
	call arrayprint('coef',real(coef)/sum(coef),coef_nmax + 2) 
c	write(4,*)'% Distribution of volumetric fraction of small particles 
c     &(D < Dbase/k2):'
c	call arrayprint('vdoksmall',vdoksmall,Ndok-1)
c	write(4,*)'% Distribution of [D43/Dbase] of small particles
c     &(D < Dbase/k2):'
c	call arrayprint('ddoksmall',ddoksmall,Ndok-1)
	write(4,*)'% Distribution of P[pocket-in-pocket] on basic
     &Dok particles sizes:'
	call arrayprint('pdoksmall',pdoksmall,Ndok-1)
c	write(4,*)'% Distribution of Zkarm on basic dok sizes:'
c	call arrayprint('zk0',gk0,Ndok)
c	call arrayprint('zk1',gk1,Ndok)
	write(4,*)
	write(4,*)'% <Conditional DOK>'
	write(4,*)'% Pockets categories sizes (mkm):'
	call arrayprint('Dkarmcat',Dpockets*1.00001e6,DPRow)
	write(4,*)'% Dependency of mass-medium Dok particles sizes on 
     &pockets categories:'
 	call arrayprint('dokkarm43',DOKP43*1.00001e6, DPRow)     
c	call arrayprint('dokkarm43(2,:)',DOKP432*1.00001e6,DPRow)
	write(4,*)'% Dependency of medium Dok particles sizes on 
     &pockets categories:'
	call arrayprint('dokkarm10',qdokkarm*1.00001e6, DPRow)	
c	write(4,*)'% Numeric density distribution function of <particles> 
c     &on pockets categories:'
c	call arrayprint('fdokcat',real(QDOK)/sum(QDOK),DPRow)
c	write(4,*)'% Accuracy of <particles> sizes modeling 
c     &on pockets categories:'
c	call arrayprint('epsfdok',Epsydok,DPRow)
c	write(4,*)'% Accuracy of <particles> distribution moments 
c     &determination (4-th and 3-th):'
c      call arrayprint('epsmdok(1,:)',Epsmdok4,DPRow)
c	call arrayprint('epsmdok(2,:)',Epsmdok3,DPRow)
	write(4,*)'% Numeric density distribution functions of  
     &particles on pockets categories:'
	do irow = 1,DPRow
	write(4,*),'% Dkarm =', int(Dpockets(irow)*1.00001e6,kind = 2),'mkm'
	write(name,'(a,I3,a)')'fqdokkarm(',irow,',:)'
	call arrayprint(trim(name), QDOKSO(irow,:)/(Di*1e6),Ndok)	
	enddo
	if (KXX.GT.1) then
	write(4,*)
	write(4,*)'%________Dependence on basic particles number_________'
		call arrayprint2('epsfkarm_n',par1, KXX)
		call arrayprint2('epsm3karm_n',par2, KXX)
		call arrayprint2('epsm4karm_n',par3, KXX)
		call arrayprint2('epsdok43_n',par4, KXX)
		call arrayprint2('epsdoksd_n',par5, KXX)
		call arrayprint2('epszkarm_n',par6, KXX)
	end if
c	вывод времени счета
	call gettim(ihr,imin,isec,i100th)
      hour1=ihr-hour
      if(hour1.lt.0)then
          hour1=24-abs(hour1)
      end if
      minut1=imin-minut
      if(minut1.lt.0)then
          hour1=hour1-1
          minut1=60-abs(minut1)
      end if
      sec1=isec-sec
      if(sec1.lt.0)then
          minut1=minut1-1
          sec1=60-abs(sec1)
      end if 
	write(4,*)    
      write(4,'(1x,a\)')'% Calculation time:'
      write(4,'(I3,a,I3,a,I3)')hour1,' :',minut1,' :',sec1
	write(4,*)'% __________________________'
	write(4,*)'hold on'
c	write(4,*)"plot(Di:Di:Di*size(fmkarm,2),fmkarm)"
c	write(4,*)"plot(Di:Di:Di*size(fmkarm_cor,2),fmkarm2,':')"
	write(4,*)"plot(Di:Di:Di*size(fmkarm,2),fmkarm)"
	write(4,*)"plot(Di:Di:Di*size(fmkarm_cor,2),fmkarm_cor,'r')"
	write(4,*)"plot(Di:Di:Di*size(fmkarm_cor2,2),fmkarm_cor2,'g:')"	

c			Вывод результатов моделирования на экран
c       #####################################################  
	write(*,*)  	
	write(*,'(1x,a\)')'Total calculation time:'
      write(*,'(I3,a,I3,a,I3)')hour1,' :',minut1,' :',sec1    
	write(*,*)
	write(*,*)'--------------------' 
      write(*,*)'Calculation results:'
	write(*,*)'--------------------' 
      write(*,*)'Total number of pockets:',QKSS
      write(*,*)'Total number of basic particles:'
      write(*,*)'NFX =',NFX
	write(*,*)'Total number of surrounding particles:'
	write(*,*)'NFZ =',NFZ
      write(*,*)'Accuracy of random numbers modeling:'
      write(*,*)(EPS1+EPS2+EPS3+EPS4+EPS5+EPS6)/6
      write(*,*)'__________' 
      write(*,*)'Accuracy of <particles> sizes modeling:'
      write(*,*)'EPSX1=',EPSX1,'  EPSX2=',EPSX2
	write(*,*)'Mass-medium diameter of <particles>:'
	write(*,*)'DokM (analytical calculation):',DOKM
      write(*,*)'Dok43 (only basic particles):',DOK43b
      write(*,*)'Dok43 (only surrounding particles):',DOK43s
      write(*,*)'Dok43 (all particles, 2 variants):',ALLDOK43, ALLDOK432
      write(*,*)'__________' 
      write(*,*)'Accuracy of <pockets> sizes modeling:',EPSY
      write(*,*)'Accuracy of <pockets> distribution moments 
     &determination (4-th and 3-th):'
      write(*,*)'EPS(MD4)=',epsMD4,'  EPS(MD3)=',epsMD3
      write(*,*)'Mass-medium size of <pockets>:'
      write(*,*)'D43(1) =',DP43
      write(*,*)'D43(2) =',Dkarm43_cor
	write(*,*)'D43(3) =',Dfmk432
c	write(*,*)'Standard deviation of <pockets> mass distribution:'
c	write(*,*)'sigma(Karm) =', SDEVP43, '(', SDEVP43/D432,' relative)'
	write(*,*)'Mass fraction of <pockets> in binder-metal 
     &composition:'
      write(*,*)'gK =', 1.-dolm1,  1.-dolm2, 1.-dolm3
	write(*,*)'<Agglomerate> size coefficient:', mp
	write(*,*)'Mass-medium size of <agglomerates>:'
      write(*,*)'Da43(1) =',DP43*mp
      write(*,*)'Da43(2) =',Dkarm43_cor*mp
	write(*,*)'Da43(3) =',Dfmk432*mp
	write(*,*)'__________'        
	write(*,*)'Results of local structure analysis:'
	write(*,*)'Medium number of bridges:', real(ibridge_total)/(FI)
	write(*,*)'Medium ratio between pockets and bridges numbers:',
     &	 nn_total/(FI)
	write(*,*)'Medium relative number of paricles in jammed pack:',
     &	 jammed_total/(FI)
	write(*,*)'Total number of conditions breakings:'
	write(*,*)'    1) Dok > Dmax      :', conditions(1)
	write(*,*)'    2) Dok < Dmin      :', conditions(2)
	write(*,*)'    3) Dbase < 0.5Dok  :', conditions(3)
	write(*,*)'    4) Dbase > 2.0Dok  :', conditions(4)
	write(*,*)'    5) l > 4.7Dbase    :', conditions(5)
	write(*,*)'    6) Nkarm = 0       :', conditions(6)
	write(*,*)'    7) Nmkm < 2        :', conditions(7)
	write(*,*)'    8) Nkarm/Nmkm < min:', conditions(8)
	write(*,*)'    9) Nkarm/Nmkm > max:', conditions(9)
      end

	subroutine arrayprint(name,array,arraysize)
	character(*) name
	integer arraysize, m
	real array(arraysize)
	m = len_trim(name)+5
	write(4,'(1x,a\)')trim(name)//' = ['
	NF = 0
	do i = 1, arraysize
		if (NF == 5) then
			if (i < arraysize) then
				write(4,'(E9.3,a)')array(i),'...'
			else
				write(4,'(E9.3,a\)')array(i),''
			endif
			NF = 0
		else
			if ((NF == 0).and.(i>1)) then
				write(4,'(<m>X,E9.3,a\)')array(i),' '
				NF = NF + 1
			else
				write(4,'(E9.3,a\)')array(i),' '
				NF = NF + 1
			endif
		endif
	end do
	write (4,*)'];'
	end subroutine

	subroutine arrayprint2(name,array,arraysize)
	character(*) name
	integer arraysize, m
	real array(arraysize)
	m = len_trim(name)+5
	write(4,'(1x,a\)')trim(name)//' = ['
	NF = 0
	do i = 1, arraysize
		if (NF == 5) then
			if (i < arraysize) then
				write(4,'(E12.6,a)')array(i),'...'
			else
				write(4,'(E12.6,a\)')array(i),''
			endif
			NF = 0
		else
			if ((NF == 0).and.(i>1)) then
				write(4,'(<m>X,E12.6,a\)')array(i),' '
				NF = NF + 1
			else
				write(4,'(E12.6,a\)')array(i),' '
				NF = NF + 1
			endif
		endif
	end do
	write (4,*)'];'
	end subroutine

      subroutine DM(x,N,Z,D,dc)
c     Моделирование размера кармана (DC)
      REAL D(N),Z(N)
      real*8 X 
      I=1
 1    CONTINUE
      IF(X.GE.Z(I).AND.X.LT.Z(I+1))THEN
      DC=D(I)+(X-Z(I))*(D(I+1)-D(I))/(Z(I+1)-Z(I))
      RETURN
      ELSE
      I=I+1
      GO TO 1
      END IF
      END

      SUBROUTINE VM(R1,R2,RK,A,JJ,VMKM,BB)
c     Определение объема МКМ
      JJ=1
      IF(R2.GT.R1) THEN
		Y1=R1
		Y2=R2
		R1=Y2
		R2=Y1
      END IF
      IF(A.GE.2*RK) THEN
		JJ=0
		RETURN
      END IF
      AB=R1+RK
      BC=R2+RK
      AC=R1+R2+A
      CosA=(AB**2+AC**2-BC**2)/(2*AB*AC)
      CosD=(AC**2+BC**2-AB**2)/(2*AC*BC)
      Q1=R1*CosA
      Q2=R2*CosD
      AL=ACOS(CosA)
      DE=ACOS(CosD)
	BB = 2*(AB*SIN(AL)-RK)
	error = BB-2*(BC*SIN(DE)-RK)
	if (BB.lt.0) then
		jj=0
		return
	endif
      BE=3.14-AL-DE
      GA=AL+BE/2. 
      R1A=R1*SIN(AL)
      R2A=R2*SIN(DE)
      Q1M=R1A*TAN(GA)
      Q2M=R2A*TAN(GA) 
      H1=R1-Q1
      H2=R2-Q2
      V1=3.14*H1**2*(R1-H1/3.)
      V2=3.14*H2**2*(R2-H2/3.)
      VMKM=3.14*R1A**2*Q1M/3-3.14*R2A**2*Q2M/3
      VMKM=VMKM-V1-V2
      Return
      END


      function random1(A,B)
	real A, B
c     Датчик случайных чисел - ГСВ №1
      random1=A+B-int(A+B)
      B=A
      A=random1
      return
      end


      subroutine random2(rndm,u0,u1,u2,u3,u4,u5,u6,u7,u8,u9)
c    Подпрограмма ГСВ №2
      integer*4 u0,u1,u2,u3,u4,u5,u6,u7,u8,u9
      integer*4 c0,c1,c2,c3,c4,c5,c6,c7,c8,c9
      integer*4 m0,m1,m2,m3,m4,m5,m6,m7,m8,m9
      integer*4 n
      real*8 x0,x1,x2,x3,x4,x5,x6,x7,x8,x9,rndm
      data m0/1/,m1/0/,m2/7916/,m3/6769/,m4/8113/,m5/7234/,
     !     m6/4142/,m7/5015/,m8/3567/,m9/1526/
      data x0/2.9387358770557187699218413430556D-39/,
     !     x1/2.4074124304840448163199724282312D-35/,
     !     x2/1.972152263052529513529321413207D-31/,
     !     x3/1.6155871338926321774832201016991D-27/,
     !     x4/1.3234889800848442797942539073119D-23/,
     !     x5/1.0842021724855044340074528008699D-19/,
     !     x6/8.8817841970012523233890533447266D-16/,
     !     x7/7.2759576141834259033203124999995D-12/,
     !     x8/5.96046447753906249999999999999962D-8/,
     !     x9/4.88281249999999999999999999999969D-4/
      c0=m0*u0
      c1=m0*u1+m1*u0
      c2=m0*u2+m1*u1+m2*u0
      c3=m0*u3+m1*u2+m2*u1+m3*u0
      c4=m0*u4+m1*u3+m2*u2+m3*u1+m4*u0
      c5=m0*u5+m1*u4+m2*u3+m3*u2+m4*u1+m5*u0
      c6=m0*u6+m1*u5+m2*u4+m3*u3+m4*u2+m5*u1+m6*u0
      c7=m0*u7+m1*u6+m2*u5+m3*u4+m4*u3+m5*u2+m6*u1+m7*u0
      c8=m0*u8+m1*u7+m2*u6+m3*u5+m4*u4+m5*u3+m6*u2+m7*u1+m8*u0
      c9=m0*u9+m1*u8+m2*u7+m3*u6+m4*u5+m5*u4+m6*u3+m7*u2+m8*u1+m9*u0
      u0=c0-ishft(ishft(c0,-13),13)
      n=c1+ishft(c0,-13) 
      u1=n-ishft(ishft(n,-13),13)
      n=c2+ishft(n,-13)
      u2=n-ishft(ishft(n,-13),13)
      n=c3+ishft(n,-13)
      u3=n-ishft(ishft(n,-13),13)
      n=c4+ishft(n,-13)
      u4=n-ishft(ishft(n,-13),13)
      n=c5+ishft(n,-13)
      u5=n-ishft(ishft(n,-13),13)
      n=c6+ishft(n,-13)
      u6=n-ishft(ishft(n,-13),13)
      n=c7+ishft(n,-13)
      u7=n-ishft(ishft(n,-13),13)
      n=c8+ishft(n,-13)
      u8=n-ishft(ishft(n,-13),13)
      n=c9+ishft(n,-13)
      u9=n-ishft(ishft(n,-11),11)
      rndm=u0*x0+u1*x1+u2*x2+u3*x3+u4*x4+u5*x5+u6*x6+u7*x7+u8*x8+u9*x9 
      end 



      SUBROUTINE PARAM(JZ,NM,G,PLOT,DOK,alfa,Z,Z1,ZS,Dmax)
      real Dmax
	REAL G(NM),DOK(2*NM), Zerror
      REAL*8 X,X1,ALFA,Z(NM),Z1(NM+1)
	INTEGER Imaxx(NM)
      IF(JZ.EQ.2) GO TO 10
      I=1
      J=I
 6    CONTINUE 
      Z(J)=G(J)*(3/3.14)*(DOK(I)+DOK(I+1))/(DOK(I)**2*DOK(I+1)**2)
       IF(I.EQ.2*NM-1) GO TO 1
      I=I+2
      J=J+1      
      GO TO 6
1     CONTINUE 
      GO TO 11    
10    CONTINUE

      I=1
      J=I
 16   CONTINUE 
      Z(J)=G(J)*(24/3.14)*(DOK(I+1)-DOK(I))/(DOK(I+1)**4-DOK(I)**4)
      IF(I.EQ.2*NM-1) GO TO 11
      I=I+2
      J=J+1      
      GO TO 16
 11   CONTINUE     

      ZS=0.
      DO 3 J=1,NM
 3    ZS=ZS+Z(J)
      DO 4 J=1,NM
 4    Z(J)=Z(J)/ZS
      Z1(1)=0.0
      DO 5 J=1,NM
 5    Z1(J+1)=Z1(J)+Z(J)

	Z1(NM+1) = 1.
	Zerror = ((1.-Z1(NM)) - Z(NM))/Z(NM)

		Imaxx = 1
1055		Dmax = 0.
			do I = 1, NM
				if ((Imaxx(I).NE.0).and.(DOK(2*I).GT.Dmax)) then
					Dmax = DOK(2*I)
					Imax = I
				endif
			enddo
c      alfa=0.00001
      x = Z1(Imax + 1) - alfa
c      DO 71 I=1,NM
c      IF(X.GE.Z1(I).AND.X.LT.Z1(I+1))MIN=I
c 71   CONTINUE
	if ((x - z1(Imax)).LT.0.) then
		Imaxx(Imax) = 0
		alfa = alfa - (z1(Imax+1) - z1(Imax))
		GO TO 1055	
	else
		x1 = (x - z1(Imax))/(z1(Imax+1)-z1(Imax))
		call SIZE(JZ,NM,X,X1,DOK,Z1,Dmax,Nfract)
	end if
      RETURN
      END



      SUBROUTINE SIZE(JZ,NM,X,X1,DOK,Z,D,MINV)
c     Подпрограмма для расчета размеров базовых и соседних частиц
      REAL DOK(2*NM)
      real*8 X,X1,Z(NM+1) 
      DO 1 I=1,NM
      IF(X.GT.Z(I).AND.X.LE.Z(I+1)) MINV=I
 1    CONTINUE
      IF(JZ.EQ.2) GO TO 2 
      D=1./SQRT(1/DOK(2*MINV-1)**2-X1*(1./DOK(2*MINV-1)**2
     &-1./DOK(2*MINV)**2))
      GO TO 3
 2    CONTINUE
      D=X1*(DOK(2*MINV)-DOK(2*MINV-1))+DOK(2*MINV-1)
 3    CONTINUE
      RETURN
      END