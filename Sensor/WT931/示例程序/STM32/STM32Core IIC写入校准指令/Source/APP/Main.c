/*
��д�ߣ�Kevin
��ַ��http://RobotControl.taobao.com
����E-mail��1609370741@qq.com
���뻷����MDK-Lite  Version: 5.17
����ʱ��: 2016-1-31
���ԣ� ���������ڡ������ǿء���STM32Coreƽ̨����ɲ���
���ܣ�
��STM32Coreƽ̨IIC�ӿڶ�ȡJY901�����ݣ�Ȼ��ͨ������1��ӡ���������֡�
����
USB-TTL����                 STM32Core              JY901
VCC          -----           VCC        ----        VCC
TX           -----           RX1     
RX           -----           TX1
GND          -----           GND        ----        GND
                             SDA2       ----        SDA
														 SCL2       ----        SCL
------------------------------------
 */
#include <string.h>
#include <stdio.h>
#include "Main.h"
#include "REG.h"
#include "stm32f10x_rcc.h"
#include "stm32f10x_gpio.h"
#include "UART1.h"
#include "delay.h"
#include "IOI2C.h"

void ShortToChar(short sData,unsigned char cData[])
{
	cData[0]=sData&0xff;
	cData[1]=sData>>8;
}
short CharToShort(unsigned char cData[])
{
	return ((short)cData[1]<<8)|cData[0];
}

int main(void)
{  
  unsigned char chrTemp[30];
	unsigned char i=0;
	unsigned char cmd1[2] = {0X00,0X01};//���Բο�һ��ʹ��˵����ԼĴ���д���ֵ
	//unsigned char cmd0[2] = {0X00,0X00};//���Բο�һ��ʹ��˵����ԼĴ���д���ֵ
	float a[3],w[3],h[3],Angle[3];
		
	SysTick_init(72,10);
	Initial_UART1(9600);
	IIC_Init();
	delay_ms(1000);//�ȴ�ģ���ʼ�����

	while (1)
	{

		delay_ms(1000);
		IICreadBytes(0x50, AX, 24,&chrTemp[0]);//��ȡ��AX�Ĵ���֮���24���ֽڳ��ȵ����ݣ�Ҳ����XYZ�ļ��ٶȽ��ٶȴų��ͽǶ�
		a[0] = (float)CharToShort(&chrTemp[0])/32768*16;//�ߵ��ֽںϲ���һ��16λ���ȵ����ݽ��м���
		a[1] = (float)CharToShort(&chrTemp[2])/32768*16;
		a[2] = (float)CharToShort(&chrTemp[4])/32768*16;
		w[0] = (float)CharToShort(&chrTemp[6])/32768*2000;
		w[1] = (float)CharToShort(&chrTemp[8])/32768*2000;
		w[2] = (float)CharToShort(&chrTemp[10])/32768*2000;
		h[0] = CharToShort(&chrTemp[12]);
		h[1] = CharToShort(&chrTemp[14]);
		h[2] = CharToShort(&chrTemp[16]);
		Angle[0] = (float)CharToShort(&chrTemp[18])/32768*180;
		Angle[1] = (float)CharToShort(&chrTemp[20])/32768*180;
		Angle[2] = (float)CharToShort(&chrTemp[22])/32768*180;
		
		printf("0x50:  a:%.3f %.3f %.3f w:%.3f %.3f %.3f  h:%.0f %.0f %.0f  Angle:%.3f %.3f %.3f \r\n",a[0],a[1],a[2],w[0],w[1],w[2],h[0],h[1],h[2],Angle[0],Angle[1],Angle[2]);		
		i++;
		if(i>10)//ÿ��10s����IICд��һ�ν��м��ٶ�У׼
		{
			i=0;
			printf("IIC���м��ٶ�У׼\r\n");
			IICwriteBytes(0x50, CALSW, 2,&cmd1[1]);
		}
	
    }
}



