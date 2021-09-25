/*
��д�ߣ�Kevin
��ַ��http://RobotControl.taobao.com
����E-mail��1609370741@qq.com
���뻷����MDK-Lite  Version: 5.17
����ʱ��: 2016-1-31
���ԣ� ���������ڡ������ǿء���STM32Coreƽ̨����ɲ���
���ܣ�
��STM32Coreƽ̨����2��ȡJY901�����ݣ�Ȼ��ͨ������1��ӡ����������,�������ֲ�����ҪѡΪ9600��
JY-901�Ĳ�����Ҫ�޸�Ϊ9600.
ע�⣺ʾ�������������ASCLL�룬��16���ƣ�HEX����ʾ�ǲ��ܿ���׼ȷ���ݵġ�
Ӳ�����ߣ�
USB-TTL����                 STM32Core              									 JY901
VCC          -----           VCC        --------------------------   VCC
TX           -----           RX1���ܽ�10��     									
RX           -----           TX1���ܽ�9��									
GND          -----           GND        --------------------------   GND
              TX2���ܽ�2��RX2���ܽ�3����Ҫת����485���ߵ�ƽ
																														A    ----  A
																														B    ----  B
------------------------------------
 */
#include <string.h>
#include <stdio.h>
#include "Main.h"
#include "stm32f10x_rcc.h"
#include "stm32f10x_gpio.h"
#include "UART1.h"
#include "UART2.h"
#include "delay.h"
#include "JY901.h"
#include "DIO.h"

struct STime		stcTime;
struct SAcc 		stcAcc;
struct SGyro 		stcGyro;
struct SAngle 	stcAngle;
struct SMag 		stcMag;
struct SDStatus stcDStatus;
struct SPress 	stcPress;
struct SLonLat 	stcLonLat;
struct SGPSV 		stcGPSV;
struct SQ       stcQ;

unsigned char cmd[8] = {0X50,0X03,0X00,0X34,0X00,0X0C,0X09,0X80};//��ȡ0X34֮���12���Ĵ���

void CharToShort(unsigned char cTemp[],short sTemp[],short sShortNum)
{
	int i;
	for (i = 0;i<3;i++) 
		sTemp[i] = (cTemp[2*i+sShortNum]<<8)|(cTemp[2*i+sShortNum+1]&0xff);
}

//CopeSerialDataΪ����2�жϵ��ú���������ÿ�յ�һ�����ݣ�����һ�����������
void CopeSerial2Data(unsigned char ucData)
{
	static unsigned char ucRxBuffer[250];
	static unsigned char ucRxCnt = 0;	
	

	ucRxBuffer[ucRxCnt++]=ucData;	//���յ������ݴ��뻺������
	if (ucRxBuffer[0]!=0x50) //����ͷ���ԣ������¿�ʼѰ��0x55����ͷ
	{
		ucRxCnt=0;
		return;
	}
	if (ucRxCnt<29) {return;}//���ݲ���29�����򷵻�
	else
	{
		ucRxCnt=0;//��ջ������������ջ��������ַ����������ݽṹ�����棬�Ӷ�ʵ�����ݵĽ�����
		CharToShort(ucRxBuffer,stcAcc.a,3);
		CharToShort(ucRxBuffer,stcGyro.w,9);
		CharToShort(ucRxBuffer,stcMag.h,15);
		CharToShort(ucRxBuffer,stcAngle.Angle,21);		
	}
}

void CopeSerial1Data(unsigned char ucData)
{	
	UART2_Put_Char(ucData);//ת������1�յ������ݸ�����2��JYģ�飩
}


int main(void)
{  
	char str[100];
		
	SysTick_init(72,10);//����ʱ��Ƶ��
	Initial_UART1(9600);//��PC�Ĵ���
	Initial_UART2(9600);//��WT901C485ģ��Ĵ���	
	
	LED_ON();
	delay_ms(1000);//�ȵ�WT901C485ģ��ʼ�����
	while(1)
	{			
		UART2_Put_String(cmd,8);//���Ͷ�ȡ0X34֮���12���Ĵ�����Ҳ���Ǽ��ٶ� ���ٶ� �ǶȺʹų�
			delay_ms(500);
		//������ٶ�
		printf("Acc:%.3f %.3f %.3f\r\n",(float)stcAcc.a[0]/32768*16,(float)stcAcc.a[1]/32768*16,(float)stcAcc.a[2]/32768*16);
			delay_ms(10);
		//������ٶ�
		printf("Gyro:%.3f %.3f %.3f\r\n",(float)stcGyro.w[0]/32768*2000,(float)stcGyro.w[1]/32768*2000,(float)stcGyro.w[2]/32768*2000);
			delay_ms(10);
		//����Ƕ�
		printf("Angle:%.3f %.3f %.3f\r\n",(float)stcAngle.Angle[0]/32768*180,(float)stcAngle.Angle[1]/32768*180,(float)stcAngle.Angle[2]/32768*180);
			delay_ms(10);
		//����ų�
		printf("Mag:%d %d %d\r\n\r\n",stcMag.h[0],stcMag.h[1],stcMag.h[2]);	
		    delay_ms(10);//�ȴ��������
	}//��ѭ��
}



