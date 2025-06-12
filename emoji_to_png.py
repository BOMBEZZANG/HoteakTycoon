#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
이모지를 PNG 파일로 변환하는 스크립트
Unity 호떡 게임용 손님 표정 및 UI 아이콘 생성기
"""

from PIL import Image, ImageDraw, ImageFont
import os
import sys

def create_emoji_png(emoji, filename, size=128, bg_color=(0, 0, 0, 0)):
    """
    이모지를 PNG 파일로 변환
    
    Args:
        emoji (str): 이모지 문자
        filename (str): 저장할 파일명
        size (int): 이미지 크기 (정사각형)
        bg_color (tuple): 배경색 (R, G, B, A) - 기본은 투명
    """
    try:
        # 투명배경 이미지 생성
        img = Image.new('RGBA', (size, size), bg_color)
        draw = ImageDraw.Draw(img)
        
        # 폰트 크기 설정 (이미지 크기의 70% 정도)
        font_size = int(size * 0.7)
        
        # 시스템 폰트 찾기 (이모지 지원 폰트)
        font_paths = [
            # Windows
            "C:/Windows/Fonts/seguiemj.ttf",  # Windows 10/11 이모지 폰트
            "C:/Windows/Fonts/NotoColorEmoji.ttf",
            # macOS
            "/System/Library/Fonts/Apple Color Emoji.ttc",
            "/Library/Fonts/Apple Color Emoji.ttc",
            # Linux
            "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        ]
        
        font = None
        for font_path in font_paths:
            if os.path.exists(font_path):
                try:
                    font = ImageFont.truetype(font_path, font_size)
                    break
                except:
                    continue
        
        # 폰트를 찾지 못한 경우 기본 폰트 사용
        if font is None:
            try:
                font = ImageFont.load_default()
                print(f"⚠️ 시스템 이모지 폰트를 찾지 못해 기본 폰트를 사용합니다: {filename}")
            except:
                print(f"❌ 폰트 로드 실패: {filename}")
                return False
        
        # 텍스트 크기 측정
        try:
            bbox = draw.textbbox((0, 0), emoji, font=font)
            text_width = bbox[2] - bbox[0]
            text_height = bbox[3] - bbox[1]
        except:
            # 구버전 Pillow 호환성
            text_width, text_height = draw.textsize(emoji, font=font)
        
        # 중앙 정렬 위치 계산
        x = (size - text_width) // 2
        y = (size - text_height) // 2
        
        # 이모지 그리기
        draw.text((x, y), emoji, font=font, fill=(0, 0, 0, 255))
        
        # PNG 파일로 저장
        img.save(filename, "PNG")
        print(f"✅ 생성 완료: {filename}")
        return True
        
    except Exception as e:
        print(f"❌ 생성 실패: {filename} - {str(e)}")
        return False

def create_simple_icon(shape, color, filename, size=128):
    """
    간단한 도형 아이콘 생성 (이모지 대신 사용)
    
    Args:
        shape (str): 도형 타입 ('triangle', 'heart', 'explosion')
        color (tuple): 색상 (R, G, B, A)
        filename (str): 저장할 파일명
        size (int): 이미지 크기
    """
    try:
        img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        
        center = size // 2
        
        if shape == 'triangle':  # 경고 삼각형
            points = [
                (center, size * 0.2),      # 위
                (size * 0.2, size * 0.8),  # 왼쪽 아래
                (size * 0.8, size * 0.8)   # 오른쪽 아래
            ]
            draw.polygon(points, fill=color)
            
        elif shape == 'heart':  # 하트
            # 하트 모양 근사치 (원 2개 + 삼각형)
            draw.ellipse([size*0.2, size*0.25, size*0.5, size*0.55], fill=color)
            draw.ellipse([size*0.5, size*0.25, size*0.8, size*0.55], fill=color)
            points = [
                (center, size * 0.8),
                (size * 0.25, size * 0.5),
                (size * 0.75, size * 0.5)
            ]
            draw.polygon(points, fill=color)
            
        elif shape == 'explosion':  # 폭발 (별 모양)
            import math
            points = []
            for i in range(8):
                angle = i * math.pi / 4
                if i % 2 == 0:
                    r = size * 0.4
                else:
                    r = size * 0.2
                x = center + r * math.cos(angle)
                y = center + r * math.sin(angle)
                points.append((x, y))
            draw.polygon(points, fill=color)
        
        img.save(filename, "PNG")
        print(f"✅ 아이콘 생성 완료: {filename}")
        return True
        
    except Exception as e:
        print(f"❌ 아이콘 생성 실패: {filename} - {str(e)}")
        return False

def main():
    """메인 함수 - 모든 이모지와 아이콘 생성"""
    
    print("🎨 Unity 호떡 게임 이모지 PNG 생성기")
    print("=" * 50)
    
    # 출력 디렉토리 생성
    output_dir = "hotteok_game_sprites"
    os.makedirs(output_dir, exist_ok=True)
    os.makedirs(os.path.join(output_dir, "customer"), exist_ok=True)
    os.makedirs(os.path.join(output_dir, "ui"), exist_ok=True)
    
    # 손님 표정 스프라이트들
    customer_emotions = {
        "customer_neutral.png": "😐",
        "customer_happy.png": "😊", 
        "customer_waiting.png": "😌",
        "customer_worried.png": "😟",
        "customer_angry.png": "😠",
        "customer_satisfied.png": "😄",
        "customer_confused.png": "😕"
    }
    
    # UI 아이콘들
    ui_icons = {
        "warning_icon.png": "⚠️",
        "heart_icon.png": "❤️", 
        "angry_icon.png": "💥",
        "sugar_hotteok_icon.png": "🥞",  # 팬케이크로 대체
        "seed_hotteok_icon.png": "🌰"    # 밤으로 대체
    }
    
    print("\n👤 손님 표정 스프라이트 생성 중...")
    success_count = 0
    
    # 손님 표정 생성
    for filename, emoji in customer_emotions.items():
        filepath = os.path.join(output_dir, "customer", filename)
        if create_emoji_png(emoji, filepath, size=128):
            success_count += 1
    
    print(f"\n📱 UI 아이콘 생성 중...")
    
    # UI 아이콘 생성
    for filename, emoji in ui_icons.items():
        filepath = os.path.join(output_dir, "ui", filename)
        if create_emoji_png(emoji, filepath, size=64):
            success_count += 1
    
    print(f"\n🎯 추가 아이콘 생성 중...")
    
    # 이모지가 제대로 안 나올 경우를 위한 대체 아이콘들
    backup_icons = [
        ("warning_triangle.png", "triangle", (255, 255, 0, 255)),  # 노란 삼각형
        ("heart_red.png", "heart", (255, 100, 100, 255)),         # 빨간 하트
        ("explosion_orange.png", "explosion", (255, 150, 0, 255))  # 주황 폭발
    ]
    
    for filename, shape, color in backup_icons:
        filepath = os.path.join(output_dir, "ui", filename)
        if create_simple_icon(shape, color, filepath, size=64):
            success_count += 1
    
    print("\n" + "=" * 50)
    print(f"🎉 생성 완료! 총 {success_count}개 파일 생성됨")
    print(f"📁 저장 위치: {os.path.abspath(output_dir)}")
    
    print(f"\n📋 Unity 사용법:")
    print(f"1. {output_dir} 폴더를 Unity 프로젝트의 Assets/Sprites/ 폴더로 복사")
    print(f"2. 모든 이미지의 Texture Type을 'Sprite (2D and UI)'로 설정")
    print(f"3. CustomerAnimator와 CustomerUI 스크립트에 연결")
    
    print(f"\n💡 팁:")
    print(f"- 이모지가 제대로 표시되지 않으면 backup 아이콘들을 사용하세요")
    print(f"- 필요에 따라 size 매개변수를 조정하여 다른 크기로 생성 가능합니다")

def create_custom_hotteok_icons():
    """호떡 전용 아이콘 생성 (더 게임답게)"""
    
    print("\n🥞 커스텀 호떡 아이콘 생성 중...")
    
    output_dir = "hotteok_game_sprites/ui"
    
    try:
        # 설탕 호떡 아이콘 (노란 원)
        img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        
        # 바깥 테두리 (갈색)
        draw.ellipse([8, 8, 56, 56], fill=(139, 69, 19, 255))
        # 안쪽 (노란색)
        draw.ellipse([12, 12, 52, 52], fill=(255, 215, 0, 255))
        # 설탕 반짝임 효과
        draw.ellipse([20, 20, 28, 28], fill=(255, 255, 255, 200))
        
        img.save(os.path.join(output_dir, "sugar_hotteok_custom.png"))
        print("✅ 설탕 호떡 커스텀 아이콘 생성")
        
        # 씨앗 호떡 아이콘 (갈색 원 + 검은 점들)
        img = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        
        # 바깥 테두리 (진한 갈색)
        draw.ellipse([8, 8, 56, 56], fill=(101, 67, 33, 255))
        # 안쪽 (밝은 갈색)
        draw.ellipse([12, 12, 52, 52], fill=(160, 82, 45, 255))
        # 씨앗들 (검은 점들)
        seeds = [(20, 25), (35, 20), (25, 35), (40, 40), (30, 45)]
        for x, y in seeds:
            draw.ellipse([x-2, y-2, x+2, y+2], fill=(0, 0, 0, 255))
        
        img.save(os.path.join(output_dir, "seed_hotteok_custom.png"))
        print("✅ 씨앗 호떡 커스텀 아이콘 생성")
        
    except Exception as e:
        print(f"❌ 커스텀 호떡 아이콘 생성 실패: {str(e)}")

if __name__ == "__main__":
    # 필요한 라이브러리 설치 확인
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        print("❌ Pillow 라이브러리가 설치되지 않았습니다!")
        print("다음 명령어로 설치하세요: pip install Pillow")
        sys.exit(1)
    
    # 메인 실행
    main()
    
    # 커스텀 호떡 아이콘도 생성
    create_custom_hotteok_icons()
    
    print(f"\n🎮 모든 작업 완료! Unity에서 즐거운 게임 개발 되세요! 🎉")